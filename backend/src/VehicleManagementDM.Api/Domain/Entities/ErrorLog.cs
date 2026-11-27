namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>One logged error response or failure (D-032, ARCHITECTURE.md §6). Deleted after 30 days.</summary>
public class ErrorLog
{
    public long Id { get; set; }

    public DateTime AtUtc { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string Code { get; set; } = string.Empty;

    public string? Method { get; set; }

    public string? Path { get; set; }

    public int? UserId { get; set; }

    public string? Role { get; set; }

    public string? ExceptionType { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    public string? InnerException { get; set; }

    public string? ClientIp { get; set; }

    public string? AppVersion { get; set; }
}
