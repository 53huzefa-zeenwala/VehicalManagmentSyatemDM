using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>Turns every unhandled exception into the JSON error format (ARCHITECTURE.md §5.3).</summary>
public sealed class GlobalExceptionHandler(ApiErrorWriter writer, IOptionsMonitor<ErrorOptions> options) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var error = ExceptionTranslator.Translate(
            exception,
            options.CurrentValue.IncludeExceptionDetails,
            ApiErrorWriter.TraceIdOf(httpContext));

        await writer.WriteAsync(httpContext, error, exception);
        return true;
    }
}
