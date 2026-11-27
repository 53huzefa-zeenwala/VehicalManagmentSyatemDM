using System.Buffers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using VehicleManagementDM.Api.Common.Errors;

namespace VehicleManagementDM.Api.Common.Storage;

/// <summary>
/// Stores photos and signatures on the server's disk; the database keeps only the generated file
/// name (D-034). Files are checked by their content, never by name or extension (ARCHITECTURE.md §11).
/// </summary>
public sealed partial class FileStore
{
    private const int BufferSize = 81_920;
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly StorageOptions _options;
    private readonly ILogger<FileStore> _logger;

    public FileStore(IOptions<StorageOptions> options, IHostEnvironment environment, ILogger<FileStore> logger)
    {
        _options = options.Value;
        _logger = logger;
        Root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(environment.ContentRootPath, _options.RootPath);
    }

    public string Root { get; }

    /// <summary>Folder name and URL segment of a kind.</summary>
    public static string Segment(FileKind kind) => kind switch
    {
        FileKind.VehiclePhoto => "vehicles",
        FileKind.StaffPhoto => "staff",
        FileKind.Signature => "signatures",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static bool TryParseSegment(string segment, out FileKind kind)
    {
        foreach (var candidate in Enum.GetValues<FileKind>())
        {
            if (string.Equals(Segment(candidate), segment, StringComparison.Ordinal))
            {
                kind = candidate;
                return true;
            }
        }

        kind = default;
        return false;
    }

    /// <summary>Only names this store generates are accepted, which rules out path tricks like "../".</summary>
    public static bool IsValidName(string name) => FileNamePattern().IsMatch(name);

    /// <summary>The URL the app uses to download a stored file.</summary>
    public static string Url(FileKind kind, string fileName) => $"/api/v1/files/{Segment(kind)}/{fileName}";

    /// <summary>
    /// Validates and stores a file, then calls <paramref name="persistName"/> (which saves the name in the
    /// database). The file only gets its final name after that succeeds; on any failure it is removed,
    /// so there are never orphan files or names pointing at missing files.
    /// </summary>
    /// <returns>The generated file name.</returns>
    public async Task<string> SaveAsync(FileKind kind, Stream content, Func<string, Task> persistName, CancellationToken cancellationToken)
    {
        var maxBytes = kind == FileKind.Signature ? _options.MaxSignatureBytes : _options.MaxPhotoBytes;
        var directory = Path.Combine(Root, Segment(kind));
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            string extension;
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                extension = await CopyValidatedAsync(kind, content, output, maxBytes, cancellationToken);
            }

            var fileName = $"{Guid.NewGuid():N}.{extension}";
            await persistName(fileName);

            File.Move(tempPath, Path.Combine(directory, fileName));
            return fileName;
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    /// <summary>Opens a stored file for reading, or returns null when it doesn't exist.</summary>
    public StoredFile? Open(FileKind kind, string fileName)
    {
        if (!IsValidName(fileName))
        {
            return null;
        }

        var path = Path.Combine(Root, Segment(kind), fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        var contentType = fileName.EndsWith(".png", StringComparison.Ordinal) ? "image/png" : "image/jpeg";
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        return new StoredFile(stream, contentType);
    }

    /// <summary>Removes a stored file (e.g. a replaced photo). Never throws; failures are logged.</summary>
    public void Delete(FileKind kind, string fileName)
    {
        if (IsValidName(fileName))
        {
            TryDelete(Path.Combine(Root, Segment(kind), fileName));
        }
    }

    private static async Task<string> CopyValidatedAsync(FileKind kind, Stream content, Stream output, long maxBytes, CancellationToken cancellationToken)
    {
        var header = new byte[PngSignature.Length];
        var headerLength = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var extension = DetectExtension(kind, header.AsSpan(0, headerLength));
        if (extension is null)
        {
            var message = kind == FileKind.Signature ? "The signature must be a PNG image." : "Only JPEG or PNG images are allowed.";
            throw new BadRequestException(ErrorCodes.InvalidFile, message, new Dictionary<string, string[]> { ["file"] = [message] });
        }

        await output.WriteAsync(header.AsMemory(0, headerLength), cancellationToken);
        long total = headerLength;

        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new PayloadTooLargeException($"The file is larger than the {DescribeSize(maxBytes)} limit.");
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return extension;
    }

    private static string? DetectExtension(FileKind kind, ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(PngSignature))
        {
            return "png";
        }

        return kind != FileKind.Signature && header.StartsWith(JpegSignature) ? "jpg" : null;
    }

    private static string DescribeSize(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024)} MB" : $"{bytes / 1024} KB";

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not delete stored file {Path}", path);
        }
    }

    [GeneratedRegex("^[0-9a-f]{32}\\.(jpg|png)$", RegexOptions.CultureInvariant)]
    private static partial Regex FileNamePattern();
}

/// <summary>An open stored file and its content type.</summary>
public sealed record StoredFile(Stream Content, string ContentType);
