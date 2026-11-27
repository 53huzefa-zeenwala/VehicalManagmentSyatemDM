using System.Text.Json.Serialization;

namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>
/// The one error body the API ever returns (D-032, ARCHITECTURE.md §5.1), sent as
/// <c>application/problem+json</c> (RFC 9457 members plus <c>code</c>, <c>errors</c>, <c>traceId</c>).
/// </summary>
public sealed class ApiError
{
    public const string ContentType = "application/problem+json";

    public required int Status { get; init; }

    /// <summary>Stable machine code the app branches on (<see cref="ErrorCodes"/>).</summary>
    public required string Code { get; init; }

    /// <summary>Short heading for an error dialog.</summary>
    public required string Title { get; init; }

    /// <summary>The real, user-readable message the app shows.</summary>
    public required string Detail { get; init; }

    /// <summary>Field errors keyed by the JSON field name; null when not a field problem.</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    /// <summary>Correlates the response with its log entry; set when the response is written.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>Exception type of an unexpected error; only when exception details are enabled (Development).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExceptionType { get; init; }
}
