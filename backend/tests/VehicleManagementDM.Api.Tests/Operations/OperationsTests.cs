using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VehicleManagementDM.Api.BackgroundJobs;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Features.Notifications;
using VehicleManagementDM.Api.Features.Reports;
using VehicleManagementDM.Api.Features.Trips.Dtos;
using VehicleManagementDM.Api.Tests.Infrastructure;
using NotificationType = VehicleManagementDM.Api.Domain.Enums.NotificationType;

namespace VehicleManagementDM.Api.Tests.Operations;

/// <summary>Reminders, notifications, dashboard, reports and the audit log (TRP-06, NTF, RPT, AUD).</summary>
[Collection(ApiCollection.Name)]
public sealed class OperationsTests(ApiFactory factory)
{
    [Fact]
    public async Task Due_trip_reminder_notifies_the_driver_and_admins_and_is_logged_once()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        await factory.WithDbAsync(db => db.Trips.Where(t => t.Id == trip.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.NextReminderAtUtc, DateTime.UtcNow.AddMinutes(-1))));

        var job = factory.Services.GetServices<IHostedService>().OfType<ReminderJob>().Single();
        await job.RunOnceAsync(CancellationToken.None);
        await job.RunOnceAsync(CancellationToken.None); // the second run must not send it again

        var staffInbox = await ApiAssert.OkAsync<NotificationPage>(await world.Staff.GetAsync("/api/v1/notifications?unreadOnly=true"));
        Assert.Single(staffInbox.Items, n => n.Type == NotificationType.TripReminder && n.TargetId == trip.Id);
        var adminInbox = await ApiAssert.OkAsync<NotificationPage>(await world.Admin.GetAsync("/api/v1/notifications?pageSize=100"));
        Assert.Single(adminInbox.Items, n => n.Type == NotificationType.TripOverdue && n.TargetId == trip.Id);

        var detail = await ApiAssert.OkAsync<TripDetailResponse>(await world.Admin.GetAsync($"/api/v1/trips/{trip.Id}"));
        Assert.Equal([1], detail.Reminders.Select(r => r.Number));
        Assert.True(detail.IsOverdue);
        Assert.True(detail.NextReminderAtUtc > DateTime.UtcNow.AddMinutes(25)); // next one in about 30 minutes
        Assert.Empty((await ApiAssert.OkAsync<TripDetailResponse>(await world.Staff.GetAsync($"/api/v1/trips/{trip.Id}"))).Reminders);
    }

    [Fact]
    public async Task Notifications_can_be_marked_read_one_by_one_or_all_at_once()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        (await world.Staff.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/cancel", new { category = "Vehicle", note = "Brakes", vehicleLotId = world.Lot.Id })).EnsureSuccessStatusCode();
        var (_, otherAdmin) = (0, await factory.AdminClientAsync());

        var inbox = await ApiAssert.OkAsync<NotificationPage>(await otherAdmin.GetAsync("/api/v1/notifications?pageSize=100"));
        var cancelled = Assert.Single(inbox.Items, n => n.Type == NotificationType.TripCancelled && n.TargetId == trip.Id);
        Assert.Contains("Brakes", cancelled.Body);

        Assert.Equal(HttpStatusCode.NoContent, (await otherAdmin.PostAsync($"/api/v1/notifications/{cancelled.Id}/read", null)).StatusCode);
        var afterOne = await ApiAssert.OkAsync<NotificationPage>(await otherAdmin.GetAsync("/api/v1/notifications?pageSize=100"));
        Assert.NotNull(afterOne.Items.Single(n => n.Id == cancelled.Id).ReadAtUtc);

        Assert.Equal(HttpStatusCode.NoContent, (await otherAdmin.PostAsync("/api/v1/notifications/read-all", null)).StatusCode);
        Assert.Equal(0, (await ApiAssert.OkAsync<NotificationPage>(await otherAdmin.GetAsync("/api/v1/notifications"))).UnreadCount);
        await ApiAssert.ErrorAsync(await world.Staff.PostAsync($"/api/v1/notifications/{cancelled.Id}/read", null), HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Dashboard_shows_live_counts_and_what_needs_attention()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        await factory.WithDbAsync(db => db.Trips.Where(t => t.Id == trip.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.NextReminderAtUtc, DateTime.UtcNow.AddMinutes(-5))));
        var lost = await world.AnotherVehicleAsync(lotId: null);

        var dashboard = await ApiAssert.OkAsync<DashboardResponse>(await world.Admin.GetAsync("/api/v1/dashboard"));

        Assert.True(dashboard.OnTripNow >= 1);
        Assert.True(dashboard.TripsToday >= 1);
        Assert.Equal(24, dashboard.TripsPerHourToday.Count);
        Assert.Contains(dashboard.NeedsAttention.OverdueTripList, t => t.Id == trip.Id);
        Assert.True(dashboard.NeedsAttention.VehiclesNotAtLot >= 1);
        Assert.True(dashboard.NeedsAttention.VehiclesNotAtLotList.Any(v => v.Id == lost.Id) || dashboard.NeedsAttention.VehiclesNotAtLot > 20);
        await ApiAssert.ErrorAsync(await world.Staff.GetAsync("/api/v1/dashboard"), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Hours_that_cross_midnight_are_split_by_GST_day_in_reports()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        (await world.Staff.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/end", new { vehicleQrToken = world.Vehicle.QrToken, returnLotId = world.Lot.Id })).EnsureSuccessStatusCode();

        // 23:00 GST on 14 Nov 2026 to 01:00 GST on 15 Nov 2026 (19:00–21:00 UTC on the 14th).
        var start = new DateTime(2026, 11, 14, 19, 0, 0, DateTimeKind.Utc);
        await factory.WithDbAsync(async db =>
        {
            await db.TripSegments.Where(s => s.TripId == trip.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.StartedAtUtc, start).SetProperty(x => x.EndedAtUtc, start.AddHours(2)));
            await db.Trips.Where(t => t.Id == trip.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.StartedAtUtc, start).SetProperty(x => x.EndedAtUtc, start.AddHours(2)));
        });

        var staff = await ApiAssert.OkAsync<StaffHoursReport>(await world.Admin.GetAsync($"/api/v1/reports/staff-hours?from=2026-11-14&to=2026-11-15&staffId={world.StaffUser.Id}"));
        var vehicles = await ApiAssert.OkAsync<VehicleHoursReport>(await world.Admin.GetAsync($"/api/v1/reports/vehicle-hours?from=2026-11-15&to=2026-11-15&vehicleId={world.Vehicle.Id}"));

        var row = Assert.Single(staff.Rows);
        Assert.Equal(7200, row.TotalSeconds);
        Assert.Equal([new DaySeconds(new DateOnly(2026, 11, 14), 3600), new DaySeconds(new DateOnly(2026, 11, 15), 3600)], row.Days);
        Assert.Equal(3600, Assert.Single(vehicles.Rows).TotalSeconds); // only the part on the 15th
    }

    [Fact]
    public async Task Lot_utilization_trip_log_and_report_ranges()
    {
        var world = await TripWorld.CreateAsync(factory);
        await world.StartedTripAsync();

        var utilization = await ApiAssert.OkAsync<LotUtilizationReport>(await world.Admin.GetAsync($"/api/v1/reports/lot-utilization?lotId={world.Lot.Id}"));
        var log = await ApiAssert.OkAsync<PagedResult<TripSummaryResponse>>(await world.Admin.GetAsync($"/api/v1/reports/trip-log?staffId={world.StaffUser.Id}"));
        var badRange = await world.Admin.GetAsync("/api/v1/reports/staff-hours?from=2026-11-15&to=2026-11-01");

        Assert.Equal(1, Assert.Single(utilization.Rows).TripsStarted);
        Assert.Single(log.Items);
        await ApiAssert.ErrorAsync(badRange, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Audit_log_lists_admin_changes_with_before_and_after()
    {
        var admin = await factory.AdminClientAsync();
        var lot = await admin.CreateLotAsync();
        (await admin.PutAsJsonAsync($"/api/v1/lots/{lot.Id}/status", new { isActive = false, rowVersion = lot.RowVersion })).EnsureSuccessStatusCode();

        var logs = await ApiAssert.OkAsync<PagedResult<AuditLogResponse>>(await admin.GetAsync($"/api/v1/audit-logs?entityType=ParkingLot&entityId={lot.Id}"));

        Assert.Equal(["Deactivate", "Create"], logs.Items.Select(l => l.Action));
        var deactivate = logs.Items[0];
        Assert.Equal("true", deactivate.Before!["isActive"]!.ToJsonString());
        Assert.Equal("false", deactivate.After!["isActive"]!.ToJsonString());
    }
}
