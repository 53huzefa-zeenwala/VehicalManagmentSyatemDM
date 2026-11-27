using VehicleManagementDM.Api.Common.Errors;

namespace VehicleManagementDM.Api.Common.Database;

/// <summary>
/// While startup migrations haven't succeeded, API requests get 503 DATABASE_NOT_READY in the JSON
/// error format instead of failing in unpredictable ways (ARCHITECTURE.md §3). Health and Swagger pass.
/// A request that arrives during startup first waits up to <c>Database:StartupWaitSeconds</c> (default 15):
/// when IIS starts the app on the first request after an idle shutdown, that request is answered normally
/// once the quick startup check finishes, instead of failing straight away.
/// </summary>
public sealed class DatabaseReadyMiddleware(RequestDelegate next, DatabaseReadiness readiness, IConfiguration configuration)
{
    private readonly TimeSpan _startupWait = TimeSpan.FromSeconds(Math.Max(0, configuration.GetValue("Database:StartupWaitSeconds", 15)));

    public async Task InvokeAsync(HttpContext context, ApiErrorWriter writer)
    {
        if (readiness.IsReady || !context.Request.Path.StartsWithSegments("/api") || await WaitForReadyAsync(context.RequestAborted))
        {
            await next(context);
            return;
        }

        context.Response.Headers.RetryAfter = "30";
        await writer.WriteAsync(context, new ApiError
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Code = ErrorCodes.DatabaseNotReady,
            Title = "Starting up",
            Detail = "The server is connecting to its database. Try again shortly.",
        });
    }

    private async Task<bool> WaitForReadyAsync(CancellationToken requestAborted)
    {
        if (_startupWait == TimeSpan.Zero)
        {
            return false;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        timeout.CancelAfter(_startupWait);
        try
        {
            await readiness.WaitAsync(timeout.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
