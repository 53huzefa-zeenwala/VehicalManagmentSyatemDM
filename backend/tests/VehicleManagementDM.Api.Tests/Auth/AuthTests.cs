using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Time;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Auth.Dtos;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Auth;

/// <summary>Cookie sessions, lockout, deactivation and password change (AUTH-01..08, D-031).</summary>
[Collection(ApiCollection.Name)]
public sealed class AuthTests(ApiFactory factory)
{
    [Fact]
    public async Task First_admin_is_seeded_with_an_ADM_id_for_the_current_GST_year()
    {
        var loginId = await factory.WithDbAsync((db, _) =>
            db.Users.AsNoTracking().Where(u => u.Role == UserRole.Admin).OrderBy(u => u.Id).Select(u => u.LoginId).FirstAsync());

        var year = Gst.Now(TimeProvider.System).Year % 100;
        Assert.Matches(new Regex($"^ADM{year:00}\\d{{5}}$"), loginId);
    }

    [Fact]
    public async Task Login_sets_a_secure_HttpOnly_SameSite_Strict_cookie_and_me_works()
    {
        var staff = await factory.CreateUserAsync(UserRole.Staff);
        var client = factory.CreateApiClient();

        var response = await client.LoginAsync(staff.LoginId.ToLowerInvariant(), ApiFactory.DefaultPassword);

        var me = await ApiAssert.OkAsync<MeResponse>(response);
        Assert.Equal(staff.LoginId, me.LoginId);
        Assert.Equal(UserRole.Staff, me.Role);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("vms.auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);

        var meAgain = await ApiAssert.OkAsync<MeResponse>(await client.GetAsync("/api/v1/auth/me"));
        Assert.Equal(staff.Id, meAgain.Id);
    }

    [Fact]
    public async Task Remember_me_makes_the_cookie_persistent()
    {
        var staff = await factory.CreateUserAsync(UserRole.Staff);

        var response = await factory.CreateApiClient().LoginAsync(staff.LoginId, ApiFactory.DefaultPassword, rememberMe: true);

        response.EnsureSuccessStatusCode();
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("vms.auth=", StringComparison.Ordinal));
        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Me_without_a_session_is_UNAUTHENTICATED()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/v1/auth/me");

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
        await ApiAssert.LoggedAsync(factory, body);
    }

    [Fact]
    public async Task Unknown_id_and_wrong_password_both_give_INVALID_CREDENTIALS()
    {
        var staff = await factory.CreateUserAsync(UserRole.Staff);
        var client = factory.CreateApiClient();

        await ApiAssert.ErrorAsync(await client.LoginAsync("DM9999999", "whatever-pass"), HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
        await ApiAssert.ErrorAsync(await client.LoginAsync(staff.LoginId, "wrong-password"), HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
    }

    [Fact]
    public async Task Fifth_failed_attempt_locks_the_account_even_for_the_right_password()
    {
        var staff = await factory.CreateUserAsync(UserRole.Staff);
        var client = factory.CreateApiClient();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await ApiAssert.ErrorAsync(await client.LoginAsync(staff.LoginId, "wrong-password"), HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
        }

        await ApiAssert.ErrorAsync(await client.LoginAsync(staff.LoginId, "wrong-password"), HttpStatusCode.Unauthorized, ErrorCodes.AccountLocked);
        await ApiAssert.ErrorAsync(await client.LoginAsync(staff.LoginId, ApiFactory.DefaultPassword), HttpStatusCode.Unauthorized, ErrorCodes.AccountLocked);
    }

    [Fact]
    public async Task Deactivated_account_cannot_sign_in()
    {
        var staff = await factory.CreateUserAsync(UserRole.Staff, isActive: false);

        var response = await factory.CreateApiClient().LoginAsync(staff.LoginId, ApiFactory.DefaultPassword);

        await ApiAssert.ErrorAsync(response, HttpStatusCode.Unauthorized, ErrorCodes.AccountDeactivated);
    }

    [Fact]
    public async Task Deactivation_ends_an_open_session_immediately()
    {
        var staff = await factory.CreateUserAsync(UserRole.Staff);
        var client = factory.CreateApiClient();
        (await client.LoginAsync(staff.LoginId, ApiFactory.DefaultPassword)).EnsureSuccessStatusCode();

        await factory.WithDbAsync(db => db.Users.Where(u => u.Id == staff.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));

        await ApiAssert.ErrorAsync(await client.GetAsync("/api/v1/auth/me"), HttpStatusCode.Unauthorized, ErrorCodes.AccountDeactivated);
        await ApiAssert.ErrorAsync(await client.GetAsync("/api/v1/auth/me"), HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Changing_password_keeps_this_session_and_ends_the_others()
    {
        var staff = await factory.CreateUserAsync(UserRole.Staff);
        var phoneA = factory.CreateApiClient();
        var phoneB = factory.CreateApiClient();
        (await phoneA.LoginAsync(staff.LoginId, ApiFactory.DefaultPassword)).EnsureSuccessStatusCode();
        (await phoneB.LoginAsync(staff.LoginId, ApiFactory.DefaultPassword)).EnsureSuccessStatusCode();

        var change = await phoneA.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = ApiFactory.DefaultPassword, newPassword = "Brand-New-Pass-2" });

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await phoneA.GetAsync("/api/v1/auth/me")).StatusCode);
        await ApiAssert.ErrorAsync(await phoneB.GetAsync("/api/v1/auth/me"), HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateApiClient().LoginAsync(staff.LoginId, "Brand-New-Pass-2")).StatusCode);
    }

    [Fact]
    public async Task Wrong_current_password_is_a_field_error()
    {
        var client = await factory.StaffClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = "not-it-at-all", newPassword = "Brand-New-Pass-2" });

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
        Assert.Contains("currentPassword", body.Errors!.Keys);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var client = await factory.StaffClientAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        await ApiAssert.ErrorAsync(await client.GetAsync("/api/v1/auth/me"), HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Staff_calling_an_admin_endpoint_is_FORBIDDEN()
    {
        var staff = await factory.StaffClientAsync();
        var admin = await factory.AdminClientAsync();

        await ApiAssert.ErrorAsync(await staff.GetAsync("/api/v1/test/admin-only"), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/test/admin-only")).StatusCode);
    }

    [Fact]
    public async Task Too_many_login_attempts_are_rate_limited()
    {
        await using var limited = await factory.WithSettingsAsync(new() { ["Auth:LoginAttemptsPerMinute"] = "2" });
        var client = factory.CreateApiClient(limited);

        await client.LoginAsync("DM9999998", "x-password");
        await client.LoginAsync("DM9999998", "x-password");
        var third = await client.LoginAsync("DM9999998", "x-password");

        await ApiAssert.ErrorAsync(third, HttpStatusCode.TooManyRequests, ErrorCodes.TooManyRequests);
    }
}
