using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Storage;

namespace VehicleManagementDM.Api.Tests.Files;

/// <summary>File validation, naming and no-orphan behaviour (D-034, ARCHITECTURE.md §11).</summary>
public sealed class FileStoreTests : IDisposable
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] WindowsExecutable = [0x4D, 0x5A, 0x90, 0, 3, 0, 0, 0, 4, 0];

    private readonly string _root = Path.Combine(Path.GetTempPath(), "vms-filestore-tests", Guid.NewGuid().ToString("N"));
    private readonly FileStore _store;

    public FileStoreTests()
    {
        var options = Options.Create(new StorageOptions { RootPath = _root, MaxPhotoBytes = 64, MaxSignatureBytes = 32 });
        _store = new FileStore(options, new HostingEnvironment { ContentRootPath = _root }, NullLogger<FileStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Theory]
    [InlineData(FileKind.VehiclePhoto, "png")]
    [InlineData(FileKind.StaffPhoto, "jpg")]
    [InlineData(FileKind.Signature, "png")]
    public async Task Valid_image_is_stored_under_a_generated_name(FileKind kind, string extension)
    {
        var bytes = extension == "png" ? Png : Jpeg;
        string? persisted = null;

        var name = await _store.SaveAsync(kind, new MemoryStream(bytes), n => { persisted = n; return Task.CompletedTask; }, CancellationToken.None);

        Assert.Equal(name, persisted);
        Assert.Matches($"^[0-9a-f]{{32}}\\.{extension}$", name);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(_root, FileStore.Segment(kind), name)));
        Assert.Empty(Directory.GetFiles(Path.Combine(_root, FileStore.Segment(kind)), "*.tmp"));
    }

    [Fact]
    public async Task Renamed_executable_is_INVALID_FILE()
    {
        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            _store.SaveAsync(FileKind.VehiclePhoto, new MemoryStream(WindowsExecutable), _ => Task.CompletedTask, CancellationToken.None));

        Assert.Equal(ErrorCodes.InvalidFile, error.Code);
        AssertNoFiles(FileKind.VehiclePhoto);
    }

    [Fact]
    public async Task Signature_must_be_png()
    {
        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            _store.SaveAsync(FileKind.Signature, new MemoryStream(Jpeg), _ => Task.CompletedTask, CancellationToken.None));

        Assert.Equal(ErrorCodes.InvalidFile, error.Code);
    }

    [Fact]
    public async Task File_over_the_limit_is_PAYLOAD_TOO_LARGE_and_nothing_is_kept()
    {
        var tooBig = Png.Concat(new byte[100]).ToArray();

        var error = await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
            _store.SaveAsync(FileKind.StaffPhoto, new MemoryStream(tooBig), _ => Task.CompletedTask, CancellationToken.None));

        Assert.Equal(ErrorCodes.PayloadTooLarge, error.Code);
        AssertNoFiles(FileKind.StaffPhoto);
    }

    [Fact]
    public async Task Failed_database_save_leaves_no_file_behind()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _store.SaveAsync(FileKind.VehiclePhoto, new MemoryStream(Png), _ => throw new InvalidOperationException("db down"), CancellationToken.None));

        AssertNoFiles(FileKind.VehiclePhoto);
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef.png", true)]
    [InlineData("0123456789abcdef0123456789abcdef.jpg", true)]
    [InlineData("0123456789ABCDEF0123456789ABCDEF.png", false)]
    [InlineData("..\\..\\web.config", false)]
    [InlineData("../secret.png", false)]
    [InlineData("0123456789abcdef0123456789abcdef.exe", false)]
    public void Only_generated_names_are_valid(string name, bool valid) => Assert.Equal(valid, FileStore.IsValidName(name));

    [Fact]
    public void Open_returns_null_for_missing_or_invalid_names()
    {
        Assert.Null(_store.Open(FileKind.VehiclePhoto, "0123456789abcdef0123456789abcdef.png"));
        Assert.Null(_store.Open(FileKind.VehiclePhoto, "../appsettings.json"));
    }

    private void AssertNoFiles(FileKind kind)
    {
        var directory = Path.Combine(_root, FileStore.Segment(kind));
        Assert.True(!Directory.Exists(directory) || Directory.GetFiles(directory).Length == 0);
    }
}
