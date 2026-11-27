using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Common.Time;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Transfers;
using VehicleManagementDM.Api.Features.Trips;
using VehicleManagementDM.Api.Features.Trips.Dtos;

namespace VehicleManagementDM.Api.Features.Reports;

/// <summary>
/// Admin dashboard, on-screen reports and the audit log (RPT-01..04, AUD-02). Hours come from trip segments
/// and transfer driving time (BR-06, TRF-05) and are split by GST calendar day (RPT-04). No exports (RPT-03).
/// </summary>
public sealed class ReportService(
    AppDbContext db,
    LotService lots,
    TripQueries tripQueries,
    TransferService transfers,
    TimeProvider timeProvider)
{
    private const int AttentionListSize = 20;

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<DashboardResponse> DashboardAsync(CancellationToken cancellationToken)
    {
        var now = Now;
        var today = DateOnly.FromDateTime(Gst.Now(timeProvider).DateTime);
        var dayStart = Hours.GstDayStartUtc(today);
        var dayEnd = Hours.GstDayStartUtc(today.AddDays(1));

        var onTrip = await db.Trips.CountAsync(t => t.Status == TripStatus.Active, cancellationToken);
        var inTransit = await db.Transfers.CountAsync(t => t.Status == TransferStatus.InTransit, cancellationToken);

        var startsToday = await db.Trips.AsNoTracking()
            .Where(t => t.StartedAtUtc >= dayStart && t.StartedAtUtc < dayEnd)
            .Select(t => t.StartedAtUtc)
            .ToListAsync(cancellationToken);
        var perHour = Enumerable.Range(0, 24)
            .Select(hour => new HourCount(hour, startsToday.Count(s => Gst.FromUtc(s).Hour == hour)))
            .ToList();

        var segmentSeconds = (await db.TripSegments.AsNoTracking()
                .Where(s => s.StartedAtUtc < dayEnd && (s.EndedAtUtc == null || s.EndedAtUtc > dayStart))
                .Select(s => new { s.StartedAtUtc, s.EndedAtUtc })
                .ToListAsync(cancellationToken))
            .Sum(s => Hours.Overlap(s.StartedAtUtc, s.EndedAtUtc ?? now, dayStart, dayEnd));
        var transferSeconds = (await db.Transfers.AsNoTracking()
                .Where(t => t.DepartedAtUtc != null && t.DepartedAtUtc < dayEnd && (t.ArrivedAtUtc == null || t.ArrivedAtUtc > dayStart))
                .Select(t => new { Start = t.DepartedAtUtc!.Value, t.ArrivedAtUtc })
                .ToListAsync(cancellationToken))
            .Sum(t => Hours.Overlap(t.Start, t.ArrivedAtUtc ?? now, dayStart, dayEnd));

        var activeLots = await lots.ListAsync(includeInactive: false, cancellationToken);
        var freeSpaces = activeLots.Sum(l => Math.Max(l.Total.Available, 0));
        var overCapacity = activeLots
            .Where(l => l.Total.IsOverCapacity || l.ByType.Any(t => t.Available < 0))
            .Select(l => new LotAttention(new LotRef(l.Id, l.Code, l.Name), l.Total.Capacity, l.Total.Occupied))
            .ToList();

        var live = await tripQueries.LiveAsync(new LiveTripsQuery(), cancellationToken);
        var overdue = live.Where(t => t.IsOverdue).ToList();
        var overdueTransfers = await db.Transfers.CountAsync(
            t => t.Status == TransferStatus.InTransit && (t.ReminderCount > 0 || t.NextReminderAtUtc <= now), cancellationToken);

        var notAtLot = db.Vehicles.AsNoTracking().Where(v => v.CurrentLotId == null && v.Status != VehicleStatus.Active);
        var notAtLotCount = await notAtLot.CountAsync(cancellationToken);
        var notAtLotList = await notAtLot
            .OrderBy(v => v.RegistrationKey)
            .Take(AttentionListSize)
            .Select(v => new VehicleRef(v.Id, v.RegistrationNumber, v.VehicleType.Category.Name, v.VehicleType.Name))
            .ToListAsync(cancellationToken);

        var waiting = await transfers.ListAsync(new TransferQuery { Status = TransferStatus.PendingDeparture, PageSize = AttentionListSize }, cancellationToken);

        return new DashboardResponse(
            onTrip,
            inTransit,
            startsToday.Count,
            segmentSeconds + transferSeconds,
            freeSpaces,
            perHour,
            new NeedsAttention(
                overdue.Count,
                overdue.Take(AttentionListSize).ToList(),
                overdueTransfers,
                notAtLotCount,
                notAtLotList,
                overCapacity,
                waiting.TotalCount,
                waiting.Items));
    }

    /// <summary>R-1: running hours per vehicle (trip segments plus transfers), split by GST day.</summary>
    public async Task<VehicleHoursReport> VehicleHoursAsync(VehicleHoursQuery query, CancellationToken cancellationToken)
    {
        var (start, end) = Range(query);
        var now = Now;

        var segments = db.TripSegments.AsNoTracking()
            .Where(s => s.StartedAtUtc < end && (s.EndedAtUtc == null || s.EndedAtUtc > start));
        var moves = db.Transfers.AsNoTracking()
            .Where(t => t.DepartedAtUtc != null && t.DepartedAtUtc < end && (t.ArrivedAtUtc == null || t.ArrivedAtUtc > start));
        if (query.LotId is { } lotId)
        {
            segments = segments.Where(s => s.Trip.OriginLotId == lotId);
            moves = moves.Where(t => t.FromLotId == lotId);
        }

        if (query.VehicleId is { } vehicleId)
        {
            segments = segments.Where(s => s.VehicleId == vehicleId);
            moves = moves.Where(t => t.VehicleId == vehicleId);
        }

        if (query.TypeId is { } typeId)
        {
            segments = segments.Where(s => s.Vehicle.VehicleTypeId == typeId);
            moves = moves.Where(t => t.Vehicle.VehicleTypeId == typeId);
        }

        if (query.CategoryId is { } categoryId)
        {
            segments = segments.Where(s => s.Vehicle.VehicleType.CategoryId == categoryId);
            moves = moves.Where(t => t.Vehicle.VehicleType.CategoryId == categoryId);
        }

        var tripTimes = await segments
            .Select(s => new Interval(s.VehicleId, s.StartedAtUtc, s.EndedAtUtc ?? now))
            .ToListAsync(cancellationToken);
        var transferTimes = await moves
            .Select(t => new Interval(t.VehicleId, t.DepartedAtUtc!.Value, t.ArrivedAtUtc ?? now))
            .ToListAsync(cancellationToken);

        var vehicleIds = tripTimes.Concat(transferTimes).Select(i => i.Key).Distinct().ToList();
        var vehicles = await db.Vehicles.AsNoTracking()
            .Where(v => vehicleIds.Contains(v.Id))
            .Select(v => new VehicleRef(v.Id, v.RegistrationNumber, v.VehicleType.Category.Name, v.VehicleType.Name))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        var rows = vehicleIds
            .Select(id =>
            {
                var trips = tripTimes.Where(i => i.Key == id).ToList();
                var moved = transferTimes.Where(i => i.Key == id).ToList();
                var tripSeconds = trips.Sum(i => Hours.Overlap(i.Start, i.End, start, end));
                var transferSeconds = moved.Sum(i => Hours.Overlap(i.Start, i.End, start, end));
                return new VehicleHoursRow(vehicles[id], tripSeconds, transferSeconds, tripSeconds + transferSeconds, Days(trips.Concat(moved), start, end));
            })
            .OrderByDescending(r => r.TotalSeconds)
            .ThenBy(r => r.Vehicle.RegistrationNumber)
            .ToList();
        return new VehicleHoursReport(rows.Sum(r => r.TotalSeconds), rows.Count, rows);
    }

    /// <summary>R-2: hours and activity per staff member (= driver hours, D-020), split by GST day.</summary>
    public async Task<StaffHoursReport> StaffHoursAsync(StaffHoursQuery query, CancellationToken cancellationToken)
    {
        var (start, end) = Range(query);
        var now = Now;

        var segments = db.TripSegments.AsNoTracking()
            .Where(s => s.StartedAtUtc < end && (s.EndedAtUtc == null || s.EndedAtUtc > start));
        var moves = db.Transfers.AsNoTracking()
            .Where(t => t.DepartedByUserId != null && t.DepartedAtUtc < end && (t.ArrivedAtUtc == null || t.ArrivedAtUtc > start));
        if (query.StaffId is { } staffId)
        {
            segments = segments.Where(s => s.StaffId == staffId);
            moves = moves.Where(t => t.DepartedByUserId == staffId);
        }

        var tripTimes = await segments
            .Select(s => new { Interval = new Interval(s.StaffId, s.StartedAtUtc, s.EndedAtUtc ?? now), s.TripId })
            .ToListAsync(cancellationToken);
        var transferTimes = await moves
            .Select(t => new Interval(t.DepartedByUserId!.Value, t.DepartedAtUtc!.Value, t.ArrivedAtUtc ?? now))
            .ToListAsync(cancellationToken);

        var staffIds = tripTimes.Select(t => t.Interval.Key).Concat(transferTimes.Select(t => t.Key)).Distinct().ToList();
        var staff = await db.Users.AsNoTracking()
            .Where(u => staffIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var rows = staffIds
            .Select(id =>
            {
                var trips = tripTimes.Where(t => t.Interval.Key == id).ToList();
                var moved = transferTimes.Where(i => i.Key == id).ToList();
                var tripSeconds = trips.Sum(t => Hours.Overlap(t.Interval.Start, t.Interval.End, start, end));
                var transferSeconds = moved.Sum(i => Hours.Overlap(i.Start, i.End, start, end));
                var user = staff[id];
                return new StaffHoursRow(
                    new StaffRef(user.Id, user.LoginId, user.FullName, user.PhotoFileName is null ? null : FileStore.Url(FileKind.StaffPhoto, user.PhotoFileName)),
                    trips.Select(t => t.TripId).Distinct().Count(),
                    moved.Count,
                    tripSeconds,
                    transferSeconds,
                    tripSeconds + transferSeconds,
                    Days(trips.Select(t => t.Interval).Concat(moved), start, end));
            })
            .OrderByDescending(r => r.TotalSeconds)
            .ThenBy(r => r.Staff.FullName)
            .ToList();
        return new StaffHoursReport(rows.Sum(r => r.TotalSeconds), rows.Count, rows);
    }

    /// <summary>R-3: current occupancy per lot and type, plus vehicle movements in the period.</summary>
    public async Task<LotUtilizationReport> LotUtilizationAsync(LotUtilizationQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(Gst.Now(timeProvider).DateTime);
        var from = query.From ?? today;
        var to = query.To ?? from;
        if (to < from)
        {
            throw Common.Errors.BadRequestException.ForField("to", "The end date is before the start date.");
        }

        var start = Hours.GstDayStartUtc(from);
        var end = Hours.GstDayStartUtc(to.AddDays(1));
        var allLots = await lots.ListAsync(includeInactive: true, cancellationToken);
        var selected = allLots.Where(l => query.LotId is null || l.Id == query.LotId).ToList();
        var lotIds = selected.Select(l => l.Id).ToList();

        var started = await db.Trips.Where(t => lotIds.Contains(t.OriginLotId) && t.StartedAtUtc >= start && t.StartedAtUtc < end)
            .GroupBy(t => t.OriginLotId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var returned = await db.Trips.Where(t => t.Status == TripStatus.Ended && t.ReturnLotId != null && lotIds.Contains(t.ReturnLotId.Value) && t.EndedAtUtc >= start && t.EndedAtUtc < end)
            .GroupBy(t => t.ReturnLotId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var transfersOut = await db.Transfers.Where(t => lotIds.Contains(t.FromLotId) && t.DepartedAtUtc >= start && t.DepartedAtUtc < end)
            .GroupBy(t => t.FromLotId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var transfersIn = await db.Transfers.Where(t => t.Status == TransferStatus.Completed && lotIds.Contains(t.ToLotId) && t.ArrivedAtUtc >= start && t.ArrivedAtUtc < end)
            .GroupBy(t => t.ToLotId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        var rows = selected.Select(l => new LotUtilizationRow(
            new LotRef(l.Id, l.Code, l.Name),
            l.IsActive,
            l.Total,
            l.ByType,
            started.GetValueOrDefault(l.Id),
            returned.GetValueOrDefault(l.Id),
            transfersOut.GetValueOrDefault(l.Id),
            transfersIn.GetValueOrDefault(l.Id))).ToList();
        return new LotUtilizationReport(from, to, rows);
    }

    /// <summary>R-4: the trip log (including cancelled and handed-over trips), with the trip-list filters.</summary>
    public Task<PagedResult<TripSummaryResponse>> TripLogAsync(TripQuery query, int adminId, CancellationToken cancellationToken) =>
        tripQueries.ListAsync(query, adminId, viewerIsAdmin: true, cancellationToken);

    /// <summary>R-5: transfer history.</summary>
    public Task<PagedResult<TransferResponse>> TransfersAsync(TransferQuery query, CancellationToken cancellationToken) =>
        transfers.ListAsync(query, cancellationToken);

    /// <summary>AUD-02: admin changes, newest first, with before/after values.</summary>
    public async Task<PagedResult<AuditLogResponse>> AuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        var logs = db.AuditLogs.AsNoTracking();
        if (query.UserId is { } userId)
        {
            logs = logs.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            logs = logs.Where(a => a.EntityType == query.EntityType);
        }

        if (query.EntityId is { } entityId)
        {
            logs = logs.Where(a => a.EntityId == entityId);
        }

        if (query.From is { } from)
        {
            var fromUtc = Hours.GstDayStartUtc(from);
            logs = logs.Where(a => a.AtUtc >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = Hours.GstDayStartUtc(to.AddDays(1));
            logs = logs.Where(a => a.AtUtc < toUtc);
        }

        var page = await logs
            .OrderByDescending(a => a.AtUtc).ThenByDescending(a => a.Id)
            .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new { Log = a, u.LoginId, u.FullName })
            .ToPageAsync(query, cancellationToken);
        return new PagedResult<AuditLogResponse>(
            page.Items.Select(x => new AuditLogResponse(
                x.Log.Id,
                x.Log.AtUtc,
                new AuditUser(x.Log.UserId, x.LoginId, x.FullName),
                x.Log.Action,
                x.Log.EntityType,
                x.Log.EntityId,
                x.Log.BeforeJson is null ? null : JsonNode.Parse(x.Log.BeforeJson),
                x.Log.AfterJson is null ? null : JsonNode.Parse(x.Log.AfterJson))).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    private static (DateTime Start, DateTime End) Range(ReportRange range) =>
        (Hours.GstDayStartUtc(range.From!.Value), Hours.GstDayStartUtc(range.To!.Value.AddDays(1)));

    private static IReadOnlyList<DaySeconds> Days(IEnumerable<Interval> intervals, DateTime start, DateTime end) =>
        intervals
            .SelectMany(i => Hours.SplitByGstDay(i.Start, i.End, start, end))
            .GroupBy(d => d.Day)
            .OrderBy(g => g.Key)
            .Select(g => new DaySeconds(g.Key, g.Sum(d => d.Seconds)))
            .ToList();

    private sealed record Interval(int Key, DateTime Start, DateTime End);
}
