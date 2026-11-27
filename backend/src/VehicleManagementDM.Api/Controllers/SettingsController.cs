using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Features.Settings;

namespace VehicleManagementDM.Api.Controllers;

/// <summary>Admin settings (SET-01): Staff ID prefix and first-reminder delay. The 30-minute repeat is fixed (D-009).</summary>
[ApiController]
[Route("api/v1/settings")]
[Authorize(Roles = Roles.Admin)]
public sealed class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<SettingsResponse> Get(CancellationToken cancellationToken) =>
        ToResponse(await db.AppSettings.AsNoTracking().SingleAsync(s => s.Id == AppSetting.SingletonId, cancellationToken));

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

        var settings = await db.AppSettings.SingleAsync(s => s.Id == AppSetting.SingletonId, cancellationToken);
        db.ExpectRowVersion(settings, request.RowVersion);
        settings.StaffIdPrefix = prefix;
        settings.ReminderDelayMinutes = request.ReminderDelayMinutes;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(settings);
    }

    private static SettingsResponse ToResponse(AppSetting settings) =>
        new(settings.StaffIdPrefix, settings.ReminderDelayMinutes, settings.ReminderRepeatMinutes, settings.RowVersion);
}
