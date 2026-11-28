using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Auth;

/// <summary>Minimum app versions (D-058): outdated apps get 426 UPDATE_REQUIRED.</summary>
[Collection(ApiCollection.Name)]
public sealed class AppVersionTests(ApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => SetMinimumsAsync(null, null);

    public Task DisposeAsync() => SetMinimumsAsync(null, null);

    private async Task SetMinimumsAsync(string? staff, string? admin)
    {
        await factory.WithDbAsync(async (db, services) =>
        {
            await db.AppSettings.Where(s => s.Id == AppSetting.SingletonId).ExecuteUpdateAsync(
                s => s.SetProperty(x => x.MinStaffAppVersion, staff).SetProperty(x => x.MinAdminAppVersion, admin));
            services.GetRequiredService<AppVersionGate>().Invalidate();
            return 0;
        });
    }

    private static HttpClient WithVersion(HttpClient client, string? version)
    {
        client.DefaultRequestHeaders.Remove(AppVersionGate.Header);
        if (version is not null)
        {
            client.DefaultRequestHeaders.Add(AppVersionGate.Header, version);
        }

        return client;
    }

    [Theory]
    [InlineData("1.0.2", "1.0.2")]
    [InlineData("1.0.2-local", "1.0.2")]
    [InlineData("1.0.2+2003", "1.0.2")]
    [InlineData("2.1", "2.1.0")]
    public void Versions_are_read_from_the_header(string header, string expected) =>
        Assert.Equal(expected, AppVersionGate.Format(AppVersionGate.Parse(header)!));

    [Fact]
    public void Unreadable_versions_are_null()
    {
        Assert.Null(AppVersionGate.Parse(null));
        Assert.Null(AppVersionGate.Parse("abc"));
    }

    [Fact]
    public async Task Without_a_minimum_every_version_works()
    {
        var staff = WithVersion(await factory.StaffClientAsync(), "0.0.1");

        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task An_outdated_staff_app_is_blocked_on_every_request_and_a_current_one_is_not()
    {
        var staff = await factory.StaffClientAsync();
        await SetMinimumsAsync("1.0.2", null);

        await ApiAssert.ErrorAsync(await WithVersion(staff, "1.0.1").GetAsync("/api/v1/auth/me"), HttpStatusCode.UpgradeRequired, ErrorCodes.UpdateRequired);
        await ApiAssert.ErrorAsync(await WithVersion(staff, null).GetAsync("/api/v1/lots"), HttpStatusCode.UpgradeRequired, ErrorCodes.UpdateRequired);
        Assert.Equal(HttpStatusCode.OK, (await WithVersion(staff, "1.0.2").GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await WithVersion(staff, "1.1.0-local").GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task An_outdated_app_can_still_sign_out()
    {
        var staff = await factory.StaffClientAsync();
        await SetMinimumsAsync("9.0.0", null);

        Assert.Equal(HttpStatusCode.NoContent, (await WithVersion(staff, "1.0.0").PostAsync("/api/v1/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task An_outdated_app_cannot_sign_in()
    {
        var user = await factory.CreateUserAsync(UserRole.Staff);
        await SetMinimumsAsync("2.0.0", null);
        var client = WithVersion(factory.CreateApiClient(), "1.9.9");

        var response = await client.LoginAsync(user.LoginId, ApiFactory.DefaultPassword);

        var error = await ApiAssert.ErrorAsync(response, HttpStatusCode.UpgradeRequired, ErrorCodes.UpdateRequired);
        Assert.Contains("2.0.0", error.Detail);
    }

    [Fact]
    public async Task The_staff_minimum_does_not_affect_admins()
    {
        var admin = WithVersion(await factory.AdminClientAsync(), "1.0.0");
        await SetMinimumsAsync("5.0.0", null);

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/settings")).StatusCode);
    }

    [Fact]
    public async Task An_admin_can_set_minimums_but_never_above_their_own_app()
    {
        var admin = WithVersion(await factory.AdminClientAsync(), "1.0.1");
        var staff = WithVersion(await factory.StaffClientAsync(), "1.0.1");
        var current = (await admin.GetFromJsonAsync<JsonObject>("/api/v1/settings"))!;

        JsonObject Body(string? minStaff, string? minAdmin) => new()
        {
            ["staffIdPrefix"] = current["staffIdPrefix"]!.GetValue<string>(),
            ["reminderDelayMinutes"] = current["reminderDelayMinutes"]!.GetValue<int>(),
            ["minStaffAppVersion"] = minStaff,
            ["minAdminAppVersion"] = minAdmin,
            ["rowVersion"] = current["rowVersion"]!.GetValue<string>(),
        };

        var tooHigh = await admin.PutAsJsonAsync("/api/v1/settings", Body("1.0.2", "1.0.5"));
        var error = await ApiAssert.ErrorAsync(tooHigh, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
        Assert.Contains("minAdminAppVersion", error.Errors!.Keys);

        var badFormat = await admin.PutAsJsonAsync("/api/v1/settings", Body("one", null));
        await ApiAssert.ErrorAsync(badFormat, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);

        var saved = await admin.PutAsJsonAsync("/api/v1/settings", Body(" 1.0.2 ", "1.0.1"));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var body = (await saved.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal("1.0.2", body["minStaffAppVersion"]!.GetValue<string>());
        Assert.Equal("1.0.1", body["minAdminAppVersion"]!.GetValue<string>());

        // Applies at once (cache refreshed on save).
        await ApiAssert.ErrorAsync(await staff.GetAsync("/api/v1/auth/me"), HttpStatusCode.UpgradeRequired, ErrorCodes.UpdateRequired);
    }
}
