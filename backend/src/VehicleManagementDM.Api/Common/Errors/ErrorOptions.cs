namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>Configuration section "Errors" (ARCHITECTURE.md §5.4).</summary>
public sealed class ErrorOptions
{
    public const string SectionName = "Errors";

    /// <summary>
    /// When true, unexpected errors send the real exception message and type to the app.
    /// True in Development only; Production sends a generic message plus the trace ID (D-033).
    /// </summary>
    public bool IncludeExceptionDetails { get; set; }
}
