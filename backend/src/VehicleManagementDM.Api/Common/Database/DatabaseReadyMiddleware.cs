using VehicleManagementDM.Api.Common.Errors;

namespace VehicleManagementDM.Api.Common.Database;

/// <summary>
/// While startup migrations haven't succeeded, API requests get 503 DATABASE_NOT_READY in the JSON
/// error format instead of failing in unpredictable ways (ARCHITECTURE.md §3). Health and Swagger pass.
/// </summary>
public sealed class DatabaseReadyMiddleware(RequestDelegate next, DatabaseReadiness readiness)
{
    public async Task InvokeAsync(HttpContext context, ApiErrorWriter writer)
    {
        if (readiness.IsReady || !context.Request.Path.StartsWithSegments("/api"))
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
}
