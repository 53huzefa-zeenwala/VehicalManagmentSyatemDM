using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VehicleManagementDM.Api.Data;

namespace VehicleManagementDM.Api.Common.Database;

/// <summary>Reports whether the database is migrated and reachable, for <c>GET /health</c>.</summary>
public sealed class DatabaseHealthCheck(DatabaseReadiness readiness, AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!readiness.IsReady)
        {
            return HealthCheckResult.Unhealthy("Startup migrations have not completed.");
        }

        return await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database can't be reached.");
    }

    /// <summary>Writes <c>{ "status": "Healthy", "database": "Healthy" }</c>; never exposes error details.</summary>
    public static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        var database = report.Entries.TryGetValue("database", out var entry) ? entry.Status.ToString() : "Unknown";
        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            new { status = report.Status.ToString(), database },
            cancellationToken: context.RequestAborted);
    }
}
