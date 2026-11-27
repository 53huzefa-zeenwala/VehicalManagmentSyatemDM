using System.ComponentModel.DataAnnotations;

namespace VehicleManagementDM.Api.Features.Settings;

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
