using System.Security.Claims;
using System.Text;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Common.Logging;

/// <summary>Builds an <see cref="ErrorLog"/> row from a request and the error sent for it (ARCHITECTURE.md §6).</summary>
public static class ErrorLogEntryFactory
{
    public const string AppVersionHeader = "X-App-Version";

    // Query parameters whose values are never written to logs.
    private static readonly string[] SensitiveQueryKeys = ["password", "token", "qr", "secret", "key"];

    public static ErrorLog Create(HttpContext context, ApiError error, Exception? exception, DateTime nowUtc)
    {
        var user = context.User;
        var isServerError = error.Status >= 500;
        _ = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

        return new ErrorLog
        {
            AtUtc = nowUtc,
            TraceId = Truncate(error.TraceId, 64)!,
            Level = isServerError ? "Error" : "Warning",
            StatusCode = error.Status,
            Code = Truncate(error.Code, 50)!,
            Method = Truncate(context.Request.Method, 10),
            Path = Truncate(SafePath(context.Request), 400),
            UserId = userId == 0 ? null : userId,
            Role = Truncate(user.FindFirstValue(ClaimTypes.Role), 30),
            ExceptionType = Truncate(exception?.GetType().FullName, 300),
            Message = Truncate(isServerError && exception is not null ? exception.Message : error.Detail, 2000)!,
            StackTrace = isServerError ? exception?.StackTrace : null,
            InnerException = isServerError ? exception?.InnerException?.ToString() : null,
            ClientIp = Truncate(context.Connection.RemoteIpAddress?.ToString(), 45),
            AppVersion = Truncate(context.Request.Headers[AppVersionHeader].ToString(), 30) is { Length: > 0 } version ? version : null,
        };
    }

    private static string SafePath(HttpRequest request)
    {
        if (!request.QueryString.HasValue)
        {
            return request.Path.Value ?? string.Empty;
        }

        var builder = new StringBuilder(request.Path.Value);
        var separator = '?';
        foreach (var (key, value) in request.Query)
        {
            var hidden = SensitiveQueryKeys.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase));
            builder.Append(separator).Append(key).Append('=').Append(hidden ? "***" : value.ToString());
            separator = '&';
        }

        return builder.ToString();
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
