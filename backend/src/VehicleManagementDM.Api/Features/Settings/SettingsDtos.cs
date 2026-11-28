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

    /// <summary>Oldest DM Staff app allowed, "major.minor.patch"; empty or null = no minimum (D-058).</summary>
    [RegularExpression(@"^\s*(\d{1,4}\.\d{1,4}\.\d{1,6})?\s*$", ErrorMessage = "Use a version like 1.0.2, or leave it empty.")]
    public string? MinStaffAppVersion { get; init; }

    /// <summary>Oldest DM Admin app allowed; empty or null = no minimum. Can't be above the caller's own app.</summary>
    [RegularExpression(@"^\s*(\d{1,4}\.\d{1,4}\.\d{1,6})?\s*$", ErrorMessage = "Use a version like 1.0.2, or leave it empty.")]
    public string? MinAdminAppVersion { get; init; }

    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

/// <summary>How many active people use one app version.</summary>
public sealed record VersionUsage(string Version, int People);

/// <summary>
/// App versions in use per app, newest first (D-058). <c>NotSeen</c> = active people whose app hasn't reported a
/// version (not signed in since, or an older APK); they are blocked by any minimum.
/// </summary>
public sealed record AppVersionsResponse(
    IReadOnlyList<VersionUsage> Staff,
    int StaffNotSeen,
    IReadOnlyList<VersionUsage> Admin,
    int AdminNotSeen);

public sealed record SettingsResponse(
    string StaffIdPrefix,
    int ReminderDelayMinutes,
    int ReminderRepeatMinutes,
    string? MinStaffAppVersion,
    string? MinAdminAppVersion,
    byte[] RowVersion);
