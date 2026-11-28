using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Settings;

namespace VehicleManagementDM.Api.Controllers;

/// <summary>
/// Admin settings (SET-01): Staff ID prefix, first-reminder delay and the minimum app versions (D-058).
/// The 30-minute repeat is fixed (D-009).
/// </summary>
[ApiController]
[Route("api/v1/settings")]
[Authorize(Roles = Roles.Admin)]
public sealed class SettingsController(AppDbContext db, AppVersionGate versionGate) : ControllerBase
{
    [HttpGet]
    public async Task<SettingsResponse> Get(CancellationToken cancellationToken) =>
        ToResponse(await db.AppSettings.AsNoTracking().SingleAsync(s => s.Id == AppSetting.SingletonId, cancellationToken));

    /// <summary>Versions in use, for choosing a minimum (D-058): the admin picks one, never types it.</summary>
    [HttpGet("app-versions")]
    public async Task<AppVersionsResponse> AppVersions(CancellationToken cancellationToken)
    {
        var rows = await db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .GroupJoin(db.UserAppVersions, u => u.Id, v => v.UserId, (u, v) => new { u.Role, Versions = v })
            .SelectMany(x => x.Versions.DefaultIfEmpty(), (x, v) => new { x.Role, Version = v == null ? null : v.Version })
            .GroupBy(x => new { x.Role, x.Version })
            .Select(g => new { g.Key.Role, g.Key.Version, People = g.Count() })
            .ToListAsync(cancellationToken);

        IReadOnlyList<VersionUsage> For(UserRole role) => rows
            .Where(r => r.Role == role && r.Version != null)
            .Select(r => new VersionUsage(r.Version!, r.People))
            .OrderByDescending(r => AppVersionGate.Parse(r.Version))
            .ToList();

        int NotSeen(UserRole role) => rows.Where(r => r.Role == role && r.Version == null).Sum(r => r.People);

        return new AppVersionsResponse(For(UserRole.Staff), NotSeen(UserRole.Staff), For(UserRole.Admin), NotSeen(UserRole.Admin));
    }

    [HttpPut]
    public async Task<SettingsResponse> Update(UpdateSettingsRequest request, CancellationToken cancellationToken)
    {
        var prefix = request.StaffIdPrefix.Trim().ToUpperInvariant();
        if (prefix == LoginIdGenerator.AdminPrefix)
        {
            throw new BadRequestException(
                ErrorCodes.InvalidIdPrefix,
                "ADM is reserved for admin IDs.",
                new Dictionary<string, string[]> { ["staffIdPrefix"] = ["ADM is reserved for admin IDs."] });
        }

        var minStaff = AppVersionGate.Parse(request.MinStaffAppVersion);
        var minAdmin = AppVersionGate.Parse(request.MinAdminAppVersion);
        if (minAdmin is not null)
        {
            // Never let an admin lock themselves out: the admin minimum can't be above the app they are using now.
            var own = AppVersionGate.Parse(Request.Headers[AppVersionGate.Header]);
            if (own is null || own < minAdmin)
            {
                var message = $"That would block your own app ({(own is null ? "unknown version" : AppVersionGate.Format(own))}). " +
                    "Install that version of DM Admin first.";
                throw new BadRequestException(
                    ErrorCodes.ValidationFailed,
                    message,
                    new Dictionary<string, string[]> { ["minAdminAppVersion"] = [message] });
            }
        }

        // A minimum must be a version some app has actually reported, so a typo or an unreleased number can't block
        // everyone. Unchanged values are always accepted.
        var current = await db.AppSettings.AsNoTracking().SingleAsync(s => s.Id == AppSetting.SingletonId, cancellationToken);
        await EnsureInUseAsync(minStaff, current.MinStaffAppVersion, UserRole.Staff, "minStaffAppVersion", cancellationToken);
        await EnsureInUseAsync(minAdmin, current.MinAdminAppVersion, UserRole.Admin, "minAdminAppVersion", cancellationToken);

        var settings = await db.AppSettings.SingleAsync(s => s.Id == AppSetting.SingletonId, cancellationToken);
        db.ExpectRowVersion(settings, request.RowVersion);
        settings.StaffIdPrefix = prefix;
        settings.ReminderDelayMinutes = request.ReminderDelayMinutes;
        settings.MinStaffAppVersion = minStaff is null ? null : AppVersionGate.Format(minStaff);
        settings.MinAdminAppVersion = minAdmin is null ? null : AppVersionGate.Format(minAdmin);
        await db.SaveChangesAsync(cancellationToken);
        versionGate.Invalidate();
        return ToResponse(settings);
    }

    private async Task EnsureInUseAsync(Version? chosen, string? current, UserRole role, string field, CancellationToken cancellationToken)
    {
        if (chosen is null || AppVersionGate.Format(chosen) == current)
        {
            return;
        }

        var text = AppVersionGate.Format(chosen);
        var inUse = await db.UserAppVersions.AnyAsync(v => v.Version == text && v.User!.Role == role, cancellationToken);
        if (!inUse)
        {
            var app = role == UserRole.Admin ? "DM Admin" : "DM Staff";
            var message = $"No {app} app with version {text} has connected yet. Pick a version from the list.";
            throw new BadRequestException(ErrorCodes.ValidationFailed, message, new Dictionary<string, string[]> { [field] = [message] });
        }
    }

    private static SettingsResponse ToResponse(AppSetting settings) =>
        new(
            settings.StaffIdPrefix,
            settings.ReminderDelayMinutes,
            settings.ReminderRepeatMinutes,
            settings.MinStaffAppVersion,
            settings.MinAdminAppVersion,
            settings.RowVersion);
}
