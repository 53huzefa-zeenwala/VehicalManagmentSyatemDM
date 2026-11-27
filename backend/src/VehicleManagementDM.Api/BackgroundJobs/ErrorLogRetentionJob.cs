using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VehicleManagementDM.Api.Common.Database;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Logging;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.BackgroundJobs;

/// <summary>Once a day, deletes error logs and fallback files older than the retention period (30 days, D-033).</summary>
public sealed class ErrorLogRetentionJob(
    IServiceScopeFactory scopeFactory,
    DatabaseReadiness readiness,
    FileErrorLog fileLog,
    ErrorLogWriter errorLogWriter,
    IOptions<ErrorLoggingOptions> options,
    TimeProvider timeProvider,
    ILogger<ErrorLogRetentionJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await readiness.WaitAsync(stoppingToken);
            using var timer = new PeriodicTimer(Interval, timeProvider);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now.AddDays(-options.Value.RetentionDays);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await db.ErrorLogs.Where(e => e.AtUtc < cutoff).ExecuteDeleteAsync(cancellationToken);
            var files = fileLog.DeleteOlderThan(cutoff);
            logger.LogInformation("Error log retention: deleted {Rows} row(s) and {Files} file(s) older than {Cutoff:u}", rows, files, cutoff);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await errorLogWriter.WriteAsync(
                new ErrorLog
                {
                    AtUtc = now,
                    TraceId = "job:error-log-retention",
                    Level = "Error",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Code = ErrorCodes.InternalError,
                    ExceptionType = exception.GetType().FullName,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.ToString(),
                },
                exception);
        }
    }
}
