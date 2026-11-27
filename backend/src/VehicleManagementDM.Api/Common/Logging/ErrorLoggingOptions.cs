namespace VehicleManagementDM.Api.Common.Logging;

/// <summary>Configuration section "ErrorLogging" (ARCHITECTURE.md §6).</summary>
public sealed class ErrorLoggingOptions
{
    public const string SectionName = "ErrorLogging";

    /// <summary>
    /// Folder for the fallback files used when the database can't be reached. Relative paths are
    /// resolved from the app's content root. In production point it outside the publish folder.
    /// </summary>
    public string FallbackDirectory { get; set; } = "App_Data/logs";

    /// <summary>Error log entries and fallback files older than this are deleted (30 days, D-033).</summary>
    public int RetentionDays { get; set; } = 30;
}
