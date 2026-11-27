using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Database;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Logging;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Notifications;

namespace VehicleManagementDM.Api.BackgroundJobs;

/// <summary>
/// Trip and transfer reminders (TRP-06, TRF-07): the first after the admin-set delay, then every 30 minutes,
/// to the driver and to every active admin, as in-app notifications plus a reminder log (D-039).
/// Runs every minute. A conditional update makes each reminder go out once, even if two runs overlap.
/// </summary>
public sealed class ReminderJob(
    IServiceScopeFactory scopeFactory,
    DatabaseReadiness readiness,
    ErrorLogWriter errorLogWriter,
    TimeProvider timeProvider,
    ILogger<ReminderJob> logger) : BackgroundService
{
    private const int BatchSize = 200;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

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
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var repeat = TimeSpan.FromMinutes(await db.AppSettings.Select(s => s.ReminderRepeatMinutes).SingleAsync(cancellationToken));
            var adminIds = await notifications.ActiveAdminIdsAsync(cancellationToken);
            var sent = new List<Notification>();

            var trips = await db.Trips.AsNoTracking()
                .Where(t => t.Status == TripStatus.Active && t.NextReminderAtUtc <= now)
                .OrderBy(t => t.NextReminderAtUtc)
                .Take(BatchSize)
                .Select(t => new { t.Id, t.NextReminderAtUtc, t.ReminderCount, t.CurrentStaffId, t.StartedAtUtc, t.CurrentVehicle.RegistrationNumber, Staff = t.CurrentStaff.FullName })
                .ToListAsync(cancellationToken);
            foreach (var trip in trips)
            {
                var next = NextDue(trip.NextReminderAtUtc!.Value, repeat, now);
                var number = trip.ReminderCount + 1;
                var claimed = await db.Trips
                    .Where(t => t.Id == trip.Id && t.Status == TripStatus.Active && t.NextReminderAtUtc == trip.NextReminderAtUtc)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ReminderCount, number).SetProperty(t => t.NextReminderAtUtc, next), cancellationToken);
                if (claimed == 0)
                {
                    continue; // ended meanwhile, or another run took it
                }

                var outFor = Duration(now - trip.StartedAtUtc);
                db.ReminderLogs.AddRange(new[] { trip.CurrentStaffId }.Concat(adminIds).Distinct()
                    .Select(userId => new ReminderLog { TripId = trip.Id, RecipientUserId = userId, ReminderNumber = number, SentAtUtc = now }));
                sent.AddRange(notifications.Add([trip.CurrentStaffId], NotificationType.TripReminder,
                    $"Trip still running: {trip.RegistrationNumber}",
                    $"{trip.RegistrationNumber} has been out for {outFor}. End the trip when the vehicle is back.",
                    "Trip", trip.Id));
                sent.AddRange(notifications.Add(adminIds, NotificationType.TripOverdue,
                    $"Overdue trip: {trip.RegistrationNumber}",
                    $"{trip.Staff} has had {trip.RegistrationNumber} out for {outFor} (reminder {number}).",
                    "Trip", trip.Id));
            }

            var transfers = await db.Transfers.AsNoTracking()
                .Where(t => t.Status == TransferStatus.InTransit && t.NextReminderAtUtc <= now)
                .OrderBy(t => t.NextReminderAtUtc)
                .Take(BatchSize)
                .Select(t => new { t.Id, t.NextReminderAtUtc, t.ReminderCount, DriverId = t.DepartedByUserId!.Value, DepartedAtUtc = t.DepartedAtUtc!.Value, t.Vehicle.RegistrationNumber, Driver = t.DepartedBy!.FullName, ToLot = t.ToLot.Name })
                .ToListAsync(cancellationToken);
            foreach (var transfer in transfers)
            {
                var next = NextDue(transfer.NextReminderAtUtc!.Value, repeat, now);
                var number = transfer.ReminderCount + 1;
                var claimed = await db.Transfers
                    .Where(t => t.Id == transfer.Id && t.Status == TransferStatus.InTransit && t.NextReminderAtUtc == transfer.NextReminderAtUtc)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ReminderCount, number).SetProperty(t => t.NextReminderAtUtc, next), cancellationToken);
                if (claimed == 0)
                {
                    continue;
                }

                var outFor = Duration(now - transfer.DepartedAtUtc);
                db.ReminderLogs.AddRange(new[] { transfer.DriverId }.Concat(adminIds).Distinct()
                    .Select(userId => new ReminderLog { TransferId = transfer.Id, RecipientUserId = userId, ReminderNumber = number, SentAtUtc = now }));
                sent.AddRange(notifications.Add([transfer.DriverId], NotificationType.TransferReminder,
                    $"Transfer still on the road: {transfer.RegistrationNumber}",
                    $"{transfer.RegistrationNumber} left {outFor} ago for {transfer.ToLot}. Confirm arrival when it gets there.",
                    "Transfer", transfer.Id));
                sent.AddRange(notifications.Add(adminIds, NotificationType.TransferOverdue,
                    $"Overdue transfer: {transfer.RegistrationNumber}",
                    $"{transfer.Driver} has been driving {transfer.RegistrationNumber} to {transfer.ToLot} for {outFor} (reminder {number}).",
                    "Transfer", transfer.Id));
            }

            if (sent.Count > 0 || db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync(cancellationToken);
                await notifications.PushAsync(sent, cancellationToken);
                logger.LogInformation("Reminders sent: {Trips} trip(s), {Transfers} transfer(s)", trips.Count, transfers.Count);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await errorLogWriter.WriteAsync(
                new ErrorLog
                {
                    AtUtc = timeProvider.GetUtcNow().UtcDateTime,
                    TraceId = "job:reminders",
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

    /// <summary>Next due time on the 30-minute cadence, skipping slots missed while the server was down.</summary>
    private static DateTime NextDue(DateTime due, TimeSpan repeat, DateTime now)
    {
        var next = due + repeat;
        while (next <= now)
        {
            next += repeat;
        }

        return next;
    }

    private static string Duration(TimeSpan span) =>
        span.TotalHours >= 1 ? $"{(int)span.TotalHours} h {span.Minutes} min" : $"{Math.Max(span.Minutes, 0)} min";
}
