using System.Diagnostics;
using System.Text.Json;
using VehicleManagementDM.Api.Common.Logging;

namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>
/// The single place that sends an error to the app and logs it (D-032). Every error path
/// (exceptions, validation, status codes, auth, rate limits, database gate) goes through here.
/// </summary>
public sealed class ApiErrorWriter(ErrorLogWriter logWriter, TimeProvider timeProvider)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string TraceIdOf(HttpContext context) => Activity.Current?.Id ?? context.TraceIdentifier;

    public async Task WriteAsync(HttpContext context, ApiError error, Exception? exception = null)
    {
        error.TraceId = TraceIdOf(context);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = error.Status;
            context.Response.ContentType = ApiError.ContentType;
            context.Response.Headers.CacheControl = "no-store";
            await JsonSerializer.SerializeAsync(context.Response.Body, error, JsonOptions, CancellationToken.None);
        }

        var entry = ErrorLogEntryFactory.Create(context, error, exception, timeProvider.GetUtcNow().UtcDateTime);
        await logWriter.WriteAsync(entry, exception);
    }
}
