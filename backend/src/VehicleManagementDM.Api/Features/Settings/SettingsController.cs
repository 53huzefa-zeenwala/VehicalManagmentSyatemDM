using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Features.Settings;

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

public sealed class UpdateSettingsRequest
{
    /// <summary>1 to 5 letters; new staff IDs are this prefix + YY + 5 digits (D-011).</summary>
    [Required(ErrorMessage = "Enter the Staff ID prefix.")]
    [RegularExpression("^[A-Za-z]{1,5}$", ErrorMessage = "The prefix must be 1 to 5 letters.")]
    public string StaffIdPrefix { get; init; } = string.Empty;

    /// <summary>Minutes from trip start to the first reminder (TRP-06): 1 minute to 24 hours.</summary>
    [Range(1, 1440, ErrorMessage = "The reminder delay must be between 1 and 1,440 minutes (24 hours).")]
    public int ReminderDelayMinutes { get; init; }

    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

public sealed record SettingsResponse(string StaffIdPrefix, int ReminderDelayMinutes, int ReminderRepeatMinutes, byte[] RowVersion);
