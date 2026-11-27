namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>The single row of admin-editable settings (SET-01, DATABASE_DESIGN.md §3.14).</summary>
public class AppSetting
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Prefix of new staff IDs. Letters only; never "ADM" (D-037).</summary>
    public string StaffIdPrefix { get; set; } = "DM";

    /// <summary>Minutes from trip start / transfer departure to the first reminder (default 120, D-043).</summary>
    public int ReminderDelayMinutes { get; set; } = 120;

    /// <summary>Minutes between later reminders (30, D-009).</summary>
    public int ReminderRepeatMinutes { get; set; } = 30;

    public byte[] RowVersion { get; set; } = [];
}
