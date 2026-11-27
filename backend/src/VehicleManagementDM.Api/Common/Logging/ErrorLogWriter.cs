using VehicleManagementDM.Api.Common.Database;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Common.Logging;

/// <summary>
/// Records every error (D-032): always to the .NET log, then to the ErrorLogs table, or to the
/// fallback file when the database is not ready, is the cause of the error, or the insert fails.
/// Never throws: logging must not break the error response.
/// </summary>
public sealed class ErrorLogWriter(
    IServiceScopeFactory scopeFactory,
    DatabaseReadiness readiness,
    FileErrorLog fileLog,
    ILogger<ErrorLogWriter> logger)
{
    public async Task WriteAsync(ErrorLog entry, Exception? exception)
    {
        LogToConsole(entry, exception);

        if (readiness.IsReady && !ErrorCodes.IsDatabaseOutage(entry.Code))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.ErrorLogs.Add(entry);
                await db.SaveChangesAsync();
                return;
            }
            catch (Exception dbException)
            {
                logger.LogWarning(dbException, "Could not write error log {TraceId} to the database; writing it to the fallback file", entry.TraceId);
            }
        }

        try
        {
            await fileLog.AppendAsync(entry);
        }
        catch (Exception fileException)
        {
            logger.LogCritical(fileException, "Could not write error log {TraceId} to the fallback file {Directory}", entry.TraceId, fileLog.Directory);
        }
    }

    private void LogToConsole(ErrorLog entry, Exception? exception)
    {
        var level = entry.StatusCode >= 500 ? LogLevel.Error : LogLevel.Warning;
        logger.Log(
            level,
            entry.StatusCode >= 500 ? exception : null,
            "{StatusCode} {Code} {Method} {Path} (trace {TraceId}, user {UserId}): {Message}",
            entry.StatusCode,
            entry.Code,
            entry.Method,
            entry.Path,
            entry.TraceId,
            entry.UserId,
            entry.Message);
    }
}
