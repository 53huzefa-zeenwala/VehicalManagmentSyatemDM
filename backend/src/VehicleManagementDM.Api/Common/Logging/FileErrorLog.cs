using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Common.Logging;

/// <summary>
/// Fallback error log used when the database can't be written to: one JSON object per line in
/// <c>errors-yyyy-MM-dd.jsonl</c> (UTC date), in <see cref="ErrorLoggingOptions.FallbackDirectory"/>.
/// </summary>
public sealed class FileErrorLog
{
    public const string FilePrefix = "errors-";
    public const string FileExtension = ".jsonl";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileErrorLog(IOptions<ErrorLoggingOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.FallbackDirectory;
        Directory = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
    }

    public string Directory { get; }

    public string PathFor(DateTime utcDate) =>
        Path.Combine(Directory, FilePrefix + utcDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + FileExtension);

    public async Task AppendAsync(ErrorLog entry)
    {
        var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
        await _lock.WaitAsync();
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            await File.AppendAllTextAsync(PathFor(entry.AtUtc), line);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Deletes fallback files older than the cutoff; returns how many were removed.</summary>
    public int DeleteOlderThan(DateTime cutoffUtc)
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return 0;
        }

        var deleted = 0;
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, FilePrefix + "*" + FileExtension))
        {
            var datePart = Path.GetFileNameWithoutExtension(file)[FilePrefix.Length..];
            if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var day)
                && day < cutoffUtc.Date)
            {
                File.Delete(file);
                deleted++;
            }
        }

        return deleted;
    }
}
