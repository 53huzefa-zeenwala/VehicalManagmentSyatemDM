using System.Net;
using Microsoft.Extensions.DependencyInjection;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Files;

/// <summary>Files are served only to signed-in users, with permission checks (ARCHITECTURE.md §11).</summary>
[Collection(ApiCollection.Name)]
public sealed class FilesEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Staff_photo_is_served_with_long_private_cache()
    {
        var name = await StoreAsync(FileKind.StaffPhoto);
        var client = await factory.StaffClientAsync();

        var response = await client.GetAsync(FileStore.Url(FileKind.StaffPhoto, name));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        var cache = response.Headers.CacheControl;
        Assert.NotNull(cache);
        Assert.True(cache.Private);
        Assert.Equal(TimeSpan.FromDays(365), cache.MaxAge);
        Assert.Contains(cache.Extensions, e => e.Name == "immutable");
        Assert.Equal(FileStoreTests.Png, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Files_need_a_session()
    {
        var name = await StoreAsync(FileKind.VehiclePhoto);

        var response = await factory.CreateApiClient().GetAsync(FileStore.Url(FileKind.VehiclePhoto, name));

        await ApiAssert.ErrorAsync(response, HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Signatures_are_for_admins_in_M1()
    {
        var name = await StoreAsync(FileKind.Signature);
        var url = FileStore.Url(FileKind.Signature, name);

        await ApiAssert.ErrorAsync(await (await factory.StaffClientAsync()).GetAsync(url), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        Assert.Equal(HttpStatusCode.OK, (await (await factory.AdminClientAsync()).GetAsync(url)).StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/files/staff/0123456789abcdef0123456789abcdef.png")]
    [InlineData("/api/v1/files/staff/appsettings.json")]
    [InlineData("/api/v1/files/secrets/0123456789abcdef0123456789abcdef.png")]
    public async Task Missing_or_invalid_files_are_FILE_NOT_FOUND(string url)
    {
        var client = await factory.StaffClientAsync();

        await ApiAssert.ErrorAsync(await client.GetAsync(url), HttpStatusCode.NotFound, ErrorCodes.FileNotFound);
    }

    private Task<string> StoreAsync(FileKind kind) =>
        factory.Services.GetRequiredService<FileStore>()
            .SaveAsync(kind, new MemoryStream(FileStoreTests.Png), _ => Task.CompletedTask, CancellationToken.None);
}
