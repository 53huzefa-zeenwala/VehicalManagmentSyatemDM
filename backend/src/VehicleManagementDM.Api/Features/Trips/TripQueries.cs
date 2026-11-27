using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Common.Time;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Trips.Dtos;

namespace VehicleManagementDM.Api.Features.Trips;

/// <summary>Read side of trips: detail, lists, live view, hours (TRP-05, TRP-10, TRP-12, STF-03, BR-06).</summary>
public sealed class TripQueries(AppDbContext db, TimeProvider timeProvider)
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// A trip with its segments. Staff may only see trips they took part in; anything else is "not found",
    /// so other people's trips can't be discovered (D-006).
    /// </summary>
    public async Task<TripDetailResponse> GetDetailAsync(int tripId, int viewerId, bool viewerIsAdmin, IReadOnlyList<Notice>? warnings, CancellationToken cancellationToken)
    {
        var trip = await db.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.OriginLot)
            .Include(t => t.ReturnLot)
            .Include(t => t.DestinationLocation).ThenInclude(l => l.Area)
            .Include(t => t.CurrentStaff)
            .Include(t => t.CurrentVehicle).ThenInclude(v => v.VehicleType).ThenInclude(vt => vt.Category)
            .Include(t => t.CurrentVehicle).ThenInclude(v => v.CurrentLot)
            .Include(t => t.Segments).ThenInclude(s => s.Staff)
            .Include(t => t.Segments).ThenInclude(s => s.Vehicle).ThenInclude(v => v.VehicleType).ThenInclude(vt => vt.Category)
            .Include(t => t.Segments).ThenInclude(s => s.OutgoingVehicleLot)
            .SingleOrDefaultAsync(t => t.Id == tripId, cancellationToken);

        if (trip is null || (!viewerIsAdmin && trip.Segments.All(s => s.StaffId != viewerId)))
        {
            throw new NotFoundException("Trip not found.");
        }

        var now = Now;
        var signature = trip.Segments.SingleOrDefault(s => s.Kind == SegmentKind.Start)?.SignatureFileName;
        IReadOnlyList<ReminderEntry> reminders = viewerIsAdmin
            ? (await db.ReminderLogs.AsNoTracking()
                    .Where(r => r.TripId == trip.Id)
                    .GroupBy(r => r.ReminderNumber)
                    .Select(g => new { Number = g.Key, SentAtUtc = g.Min(r => r.SentAtUtc) })
                    .OrderBy(r => r.Number)
                    .ToListAsync(cancellationToken))
                .Select(r => new ReminderEntry(r.Number, r.SentAtUtc))
                .ToList()
            : [];
        return new TripDetailResponse(
            trip.Id,
            trip.Status,
            trip.StartedAtUtc,
            trip.EndedAtUtc,
            trip.EndedAtUtc is { } ended ? (long)(ended - trip.StartedAtUtc).TotalSeconds : null,
            IsOverdue(trip, now),
            trip.NextReminderAtUtc,
            Lot(trip.OriginLot),
            new DestinationRef(trip.DestinationLocationId, trip.DestinationLocation.Area.Name, trip.DestinationLocation.Name),
            Staff(trip.CurrentStaff),
            Card(trip.CurrentVehicle),
            signature is null ? null : FileStore.Url(FileKind.Signature, signature),
            trip.Segments.OrderBy(s => s.Seq).Select(s => new SegmentResponse(
                s.Seq,
                s.Kind,
                Staff(s.Staff),
                VehicleRef(s.Vehicle),
                s.StartedAtUtc,
                s.EndedAtUtc,
                s.Reason,
                s.OutgoingVehicleLot is null ? null : Lot(s.OutgoingVehicleLot),
                s.Kind == SegmentKind.VehicleSwap && s.OutgoingVehicleLotId is null)).ToList(),
            trip.ReturnLot is null ? null : Lot(trip.ReturnLot),
            trip.CancelCategory is { } category ? new CancelInfo(category, trip.CancelNote ?? string.Empty) : null,
            warnings ?? [],
            reminders);
    }

    /// <summary>The staff member's running trip, if any (TRP-04).</summary>
    public async Task<TripDetailResponse?> GetActiveForStaffAsync(int staffId, CancellationToken cancellationToken)
    {
        var tripId = await db.Trips
            .Where(t => t.Status == TripStatus.Active && t.CurrentStaffId == staffId)
            .Select(t => (int?)t.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return tripId is null ? null : await GetDetailAsync(tripId.Value, staffId, viewerIsAdmin: false, null, cancellationToken);
    }

    /// <summary>Trip history (TRP-12): staff see trips they took part in; admins see all, with filters.</summary>
    public Task<PagedResult<TripSummaryResponse>> ListAsync(TripQuery query, int viewerId, bool viewerIsAdmin, CancellationToken cancellationToken)
    {
        var trips = db.Trips.AsNoTracking();
        if (!viewerIsAdmin)
        {
            trips = trips.Where(t => t.Segments.Any(s => s.StaffId == viewerId));
        }
        else if (query.StaffId is { } staffId)
        {
            trips = trips.Where(t => t.Segments.Any(s => s.StaffId == staffId));
        }

        if (query.From is { } from)
        {
            var fromUtc = GstDayStartUtc(from);
            trips = trips.Where(t => t.StartedAtUtc >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = GstDayStartUtc(to.AddDays(1));
            trips = trips.Where(t => t.StartedAtUtc < toUtc);
        }

        if (query.Status is { } status)
        {
            trips = trips.Where(t => t.Status == status);
        }

        if (query.VehicleId is { } vehicleId)
        {
            trips = trips.Where(t => t.Segments.Any(s => s.VehicleId == vehicleId));
        }

        if (query.LotId is { } lotId)
        {
            trips = trips.Where(t => t.OriginLotId == lotId);
        }

        return Summaries(trips.OrderByDescending(t => t.StartedAtUtc).ThenByDescending(t => t.Id)).ToPageAsync(query, cancellationToken);
    }

    /// <summary>Every active trip, overdue first (TRP-05, A-02). No auto-refresh (D-035).</summary>
    public async Task<IReadOnlyList<TripSummaryResponse>> LiveAsync(LiveTripsQuery query, CancellationToken cancellationToken)
    {
        var now = Now;
        var trips = db.Trips.AsNoTracking().Where(t => t.Status == TripStatus.Active);
        if (query.LotId is { } lotId)
        {
            trips = trips.Where(t => t.OriginLotId == lotId);
        }

        if (query.TypeId is { } typeId)
        {
            trips = trips.Where(t => t.CurrentVehicle.VehicleTypeId == typeId);
        }

        var ordered = trips
            .OrderByDescending(t => t.ReminderCount > 0 || t.NextReminderAtUtc <= now)
            .ThenBy(t => t.StartedAtUtc);
        return await Summaries(ordered).ToListAsync(cancellationToken);
    }

    /// <summary>A staff member's trips and their hours in the period, including transfers they drove (STF-03, BR-06, TRF-05).</summary>
    public async Task<StaffTripsResponse> StaffTripsAsync(int staffId, HoursQuery query, CancellationToken cancellationToken)
    {
        var staff = await db.Users.AsNoTracking()
            .Where(u => u.Id == staffId && u.Role == UserRole.Staff)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Staff member not found.");

        var rangeStart = query.From is { } from ? GstDayStartUtc(from) : DateTime.MinValue;
        var rangeEnd = query.To is { } to ? GstDayStartUtc(to.AddDays(1)) : DateTime.MaxValue;
        var seconds = await SegmentSecondsAsync(staffId, rangeStart, rangeEnd, cancellationToken);

        var trips = await ListAsync(
            new TripQuery { StaffId = staffId, From = query.From, To = query.To, Page = query.Page, PageSize = query.PageSize },
            staffId,
            viewerIsAdmin: true,
            cancellationToken);
        return new StaffTripsResponse(Staff(staff), seconds, trips);
    }

    /// <summary>Trips taken part in and hours driven in the current GST calendar month (Profile, Home).</summary>
    public async Task<MonthStats> MonthStatsAsync(int staffId, CancellationToken cancellationToken)
    {
        var today = Gst.Now(timeProvider);
        var monthStart = GstDayStartUtc(new DateOnly(today.Year, today.Month, 1));
        var monthEnd = GstDayStartUtc(new DateOnly(today.Year, today.Month, 1).AddMonths(1));
        var now = Now;

        var segments = await db.TripSegments.AsNoTracking()
            .Where(s => s.StaffId == staffId && s.StartedAtUtc < monthEnd && (s.EndedAtUtc == null || s.EndedAtUtc > monthStart))
            .Select(s => new { s.TripId, s.StartedAtUtc, s.EndedAtUtc })
            .ToListAsync(cancellationToken);
        var seconds = segments.Sum(s => Hours.Overlap(s.StartedAtUtc, s.EndedAtUtc ?? now, monthStart, monthEnd))
            + await TransferSecondsAsync(staffId, monthStart, monthEnd, cancellationToken);
        return new MonthStats(segments.Select(s => s.TripId).Distinct().Count(), seconds);
    }

    /// <summary>Active staff who could take over a trip: not the caller, not on a trip, not driving a transfer (TRP-07, D-026, D-041).</summary>
    public async Task<IReadOnlyList<StaffRef>> HandoverCandidatesAsync(int callerId, string? search, CancellationToken cancellationToken)
    {
        var staff = db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Staff && u.IsActive && u.Id != callerId)
            .Where(u => !db.Trips.Any(t => t.Status == TripStatus.Active && t.CurrentStaffId == u.Id))
            .Where(u => !db.Transfers.Any(t => t.Status == TransferStatus.InTransit && t.DepartedByUserId == u.Id));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            staff = staff.Where(u => u.FullName.Contains(text) || u.LoginId.Contains(text));
        }

        var users = await staff.OrderBy(u => u.FullName).Take(50).ToListAsync(cancellationToken);
        return users.Select(Staff).ToList();
    }

    public static bool IsOverdue(Trip trip, DateTime nowUtc) =>
        trip.Status == TripStatus.Active && (trip.ReminderCount > 0 || trip.NextReminderAtUtc <= nowUtc);

    public static StaffRef Staff(User user) =>
        new(user.Id, user.LoginId, user.FullName, user.PhotoFileName is null ? null : FileStore.Url(FileKind.StaffPhoto, user.PhotoFileName));

    public static VehicleCardResponse Card(Vehicle vehicle) => new(
        vehicle.Id,
        vehicle.RegistrationNumber,
        vehicle.VehicleType.Category.Name,
        vehicle.VehicleType.Name,
        vehicle.Brand,
        vehicle.Model,
        vehicle.Colour,
        vehicle.PhotoFileName is null ? null : FileStore.Url(FileKind.VehiclePhoto, vehicle.PhotoFileName),
        vehicle.Status,
        vehicle.CurrentLot is null ? null : Lot(vehicle.CurrentLot));

    public static LotRef Lot(ParkingLot lot) => new(lot.Id, lot.Code, lot.Name);

    /// <summary>UTC instant when a GST calendar day starts (00:00 GST = 20:00 UTC the day before).</summary>
    public static DateTime GstDayStartUtc(DateOnly day) => Hours.GstDayStartUtc(day);

    private static VehicleRef VehicleRef(Vehicle vehicle) =>
        new(vehicle.Id, vehicle.RegistrationNumber, vehicle.VehicleType.Category.Name, vehicle.VehicleType.Name);

    private async Task<long> SegmentSecondsAsync(int staffId, DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken)
    {
        var now = Now;
        var segments = await db.TripSegments.AsNoTracking()
            .Where(s => s.StaffId == staffId && s.StartedAtUtc < rangeEnd && (s.EndedAtUtc == null || s.EndedAtUtc > rangeStart))
            .Select(s => new { s.StartedAtUtc, s.EndedAtUtc })
            .ToListAsync(cancellationToken);
        return segments.Sum(s => Hours.Overlap(s.StartedAtUtc, s.EndedAtUtc ?? now, rangeStart, rangeEnd))
            + await TransferSecondsAsync(staffId, rangeStart, rangeEnd, cancellationToken);
    }

    /// <summary>Time spent driving transfers (TRF-05): from confirmed departure to arrival (or now).</summary>
    private async Task<long> TransferSecondsAsync(int staffId, DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken)
    {
        var now = Now;
        var transfers = await db.Transfers.AsNoTracking()
            .Where(t => t.DepartedByUserId == staffId && t.DepartedAtUtc < rangeEnd && (t.ArrivedAtUtc == null || t.ArrivedAtUtc > rangeStart))
            .Select(t => new { Start = t.DepartedAtUtc!.Value, t.ArrivedAtUtc })
            .ToListAsync(cancellationToken);
        return transfers.Sum(t => Hours.Overlap(t.Start, t.ArrivedAtUtc ?? now, rangeStart, rangeEnd));
    }

    private IQueryable<TripSummaryResponse> Summaries(IQueryable<Trip> trips)
    {
        var now = Now;
        return trips.Select(t => new TripSummaryResponse(
            t.Id,
            t.Status,
            t.StartedAtUtc,
            t.EndedAtUtc,
            t.EndedAtUtc == null ? null : (long?)EF.Functions.DateDiffSecond(t.StartedAtUtc, t.EndedAtUtc.Value),
            t.Status == TripStatus.Active && (t.ReminderCount > 0 || t.NextReminderAtUtc <= now),
            new LotRef(t.OriginLot.Id, t.OriginLot.Code, t.OriginLot.Name),
            t.ReturnLot == null ? null : new LotRef(t.ReturnLot.Id, t.ReturnLot.Code, t.ReturnLot.Name),
            new DestinationRef(t.DestinationLocationId, t.DestinationLocation.Area.Name, t.DestinationLocation.Name),
            new StaffRef(
                t.CurrentStaff.Id,
                t.CurrentStaff.LoginId,
                t.CurrentStaff.FullName,
                t.CurrentStaff.PhotoFileName == null ? null : FileStore.Url(FileKind.StaffPhoto, t.CurrentStaff.PhotoFileName)),
            new VehicleRef(t.CurrentVehicle.Id, t.CurrentVehicle.RegistrationNumber, t.CurrentVehicle.VehicleType.Category.Name, t.CurrentVehicle.VehicleType.Name),
            t.Segments.Count));
    }
}
