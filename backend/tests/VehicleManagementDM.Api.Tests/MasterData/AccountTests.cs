using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Features.Accounts;
using VehicleManagementDM.Api.Features.Auth.Dtos;
using VehicleManagementDM.Api.Features.Settings;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.MasterData;

/// <summary>Staff, admins and settings (STF-01..02, AUTH-03..07, SET-01, D-011, D-037).</summary>
[Collection(ApiCollection.Name)]
public sealed class AccountTests(ApiFactory factory)
{
    [Fact]
    public async Task New_staff_gets_a_DM_id_and_can_sign_in()
    {
        var admin = await factory.AdminClientAsync();

        var staff = await admin.CreateStaffAsync();
        var client = factory.CreateApiClient();
        var me = await ApiAssert.OkAsync<MeResponse>(await client.LoginAsync(staff.LoginId, ApiFactory.DefaultPassword));

        Assert.Matches(new Regex(@"^DM\d{2}\d{5}$"), staff.LoginId);
        Assert.Equal(staff.Id, me.Id);
        Assert.False(me.HasActiveTrip);
        var found = await ApiAssert.OkAsync<PagedResult<StaffResponse>>(await admin.GetAsync($"/api/v1/staff?search={staff.LoginId}"));
        Assert.Equal([staff.Id], found.Items.Select(s => s.Id));
    }

    [Fact]
    public async Task Updating_staff_changes_details_and_checks_the_row_version()
    {
        var admin = await factory.AdminClientAsync();
        var staff = await admin.CreateStaffAsync();

        var updated = await ApiAssert.OkAsync<StaffResponse>(await admin.PutAsJsonAsync($"/api/v1/staff/{staff.Id}", new
        {
            fullName = staff.FullName,
            mobile = "+971500000099",
            licenceNumber = "lic-new-1",
            rowVersion = staff.RowVersion,
        }));
        var stale = await admin.PutAsJsonAsync($"/api/v1/staff/{staff.Id}", new
        {
            fullName = staff.FullName,
            mobile = staff.Mobile,
            licenceNumber = staff.LicenceNumber,
            rowVersion = staff.RowVersion,
        });

        Assert.Equal(("+971500000099", "LIC-NEW-1"), (updated.Mobile, updated.LicenceNumber));
        await ApiAssert.ErrorAsync(stale, HttpStatusCode.Conflict, ErrorCodes.ConcurrencyConflict);
    }

    [Fact]
    public async Task Deactivating_staff_ends_their_session_at_once_and_is_audited()
    {
        var admin = await factory.AdminClientAsync();
        var staff = await admin.CreateStaffAsync();
        var phone = factory.CreateApiClient();
        (await phone.LoginAsync(staff.LoginId, ApiFactory.DefaultPassword)).EnsureSuccessStatusCode();

        var result = await ApiAssert.OkAsync<StaffResponse>(await admin.PutAsJsonAsync($"/api/v1/staff/{staff.Id}/status", new { isActive = false, rowVersion = staff.RowVersion }));

        Assert.False(result.IsActive);
        await ApiAssert.ErrorAsync(await phone.GetAsync("/api/v1/auth/me"), HttpStatusCode.Unauthorized, ErrorCodes.AccountDeactivated);
        var actions = await factory.WithDbAsync((db, _) =>
            db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "User" && a.EntityId == staff.Id).Select(a => a.Action).ToListAsync());
        Assert.Contains("Deactivate", actions);
    }

    [Fact]
    public async Task Password_reset_ends_sessions_and_is_audited_without_the_password()
    {
        var admin = await factory.AdminClientAsync();
        var staff = await admin.CreateStaffAsync();
        var phone = factory.CreateApiClient();
        (await phone.LoginAsync(staff.LoginId, ApiFactory.DefaultPassword)).EnsureSuccessStatusCode();

        var reset = await admin.PostAsJsonAsync($"/api/v1/staff/{staff.Id}/reset-password", new { newPassword = "Reset-Pass-123" });

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        await ApiAssert.ErrorAsync(await phone.GetAsync("/api/v1/auth/me"), HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateApiClient().LoginAsync(staff.LoginId, "Reset-Pass-123")).StatusCode);
        var audit = await factory.WithDbAsync((db, _) =>
            db.AuditLogs.AsNoTracking().SingleAsync(a => a.EntityType == "User" && a.EntityId == staff.Id && a.Action == "PasswordReset"));
        Assert.Null(audit.BeforeJson);
        Assert.Null(audit.AfterJson);
    }

    [Fact]
    public async Task Admins_get_ADM_ids_and_cannot_deactivate_themselves()
    {
        var admin = await factory.AdminClientAsync();
        var me = await ApiAssert.OkAsync<MeResponse>(await admin.GetAsync("/api/v1/auth/me"));
        var self = await ApiAssert.OkAsync<AdminResponse>(await admin.GetAsync($"/api/v1/admins/{me.Id}"));

        var created = await ApiAssert.OkAsync<AdminResponse>(await admin.PostAsJsonAsync("/api/v1/admins", new
        {
            fullName = MasterDataHelpers.Unique("Admin"),
            mobile = "+971500000040",
            password = "Second-Admin-1",
        }));
        var selfDeactivate = await admin.PutAsJsonAsync($"/api/v1/admins/{me.Id}/status", new { isActive = false, rowVersion = self.RowVersion });

        Assert.Matches(new Regex(@"^ADM\d{2}\d{5}$"), created.LoginId);
        await ApiAssert.ErrorAsync(selfDeactivate, HttpStatusCode.Conflict, ErrorCodes.CannotDeactivateSelf);
    }

    [Fact]
    public async Task Staff_id_prefix_setting_applies_to_new_staff_and_ADM_is_reserved()
    {
        var admin = await factory.AdminClientAsync();
        var settings = await ApiAssert.OkAsync<SettingsResponse>(await admin.GetAsync("/api/v1/settings"));
        try
        {
            var reserved = await admin.PutAsJsonAsync("/api/v1/settings", new { staffIdPrefix = "adm", reminderDelayMinutes = 120, rowVersion = settings.RowVersion });
            await ApiAssert.ErrorAsync(reserved, HttpStatusCode.BadRequest, ErrorCodes.InvalidIdPrefix);

            settings = await ApiAssert.OkAsync<SettingsResponse>(await admin.PutAsJsonAsync(
                "/api/v1/settings", new { staffIdPrefix = "xy", reminderDelayMinutes = 90, rowVersion = settings.RowVersion }));
            var staff = await admin.CreateStaffAsync();

            Assert.Equal(("XY", 90, 30), (settings.StaffIdPrefix, settings.ReminderDelayMinutes, settings.ReminderRepeatMinutes));
            Assert.StartsWith("XY", staff.LoginId);
        }
        finally
        {
            var current = await ApiAssert.OkAsync<SettingsResponse>(await admin.GetAsync("/api/v1/settings"));
            (await admin.PutAsJsonAsync("/api/v1/settings", new { staffIdPrefix = "DM", reminderDelayMinutes = 120, rowVersion = current.RowVersion })).EnsureSuccessStatusCode();
        }
    }
}
