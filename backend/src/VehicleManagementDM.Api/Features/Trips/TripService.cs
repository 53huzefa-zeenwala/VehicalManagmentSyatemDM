using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Database;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots;
using VehicleManagementDM.Api.Features.Notifications;
using VehicleManagementDM.Api.Features.Trips.Dtos;
using VehicleManagementDM.Api.Features.Vehicles;

namespace VehicleManagementDM.Api.Features.Trips;

/// <summary>
/// Trip actions by staff (TRP-01..09). Admins monitor only (D-021). Every action runs in one transaction
/// under application locks on the staff member(s), vehicle(s) and trip involved, so two phones can never
/// take the same vehicle or start two trips for one person; the filtered unique indexes are the final guard.
/// </summary>
public sealed class TripService(
    AppDbContext db,
    FileStore fileStore,
    LotService lots,
    TripQueries queries,
    NotificationService notifications,
    TimeProvider timeProvider)
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>Vehicle card for a scanned QR code or typed registration, and whether this staff member can start it now.</summary>
    public async Task<VehicleLookupResponse> LookupAsync(int staffId, VehicleLookupQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await FindVehicleAsync(query.Qr, query.Registration, tracked: false, cancellationToken);
        var block = StartBlock(vehicle)
            ?? await TransferBlockAsync(vehicle, cancellationToken)
            ?? await ActiveTripBlockAsync(staffId, cancellationToken);
        return new VehicleLookupResponse(TripQueries.Card(vehicle), block is null, block);
    }

    /// <summary>Starts a trip: vehicle (scan or type) → destination → staff signature (TRP-01..03).</summary>
    public async Task<TripDetailResponse> StartAsync(int staffId, StartTripRequest request, CancellationToken cancellationToken)
    {
        var signature = DecodeSignature(request.SignaturePngBase64);
        var identified = await FindVehicleAsync(request.VehicleQrToken, request.VehicleRegistration, tracked: false, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Staff(staffId), AppLocks.Vehicle(identified.Id));

        ThrowIfBlocked(await ActiveTripBlockAsync(staffId, cancellationToken));
        var vehicle = await LoadVehicleAsync(identified.Id, cancellationToken);
        ThrowIfBlocked(StartBlock(vehicle)); // the origin is always the active lot the vehicle is parked at (D-047)
        ThrowIfBlocked(await TransferBlockAsync(vehicle, cancellationToken));
        await EnsureDestinationAsync(request.DestinationLocationId, cancellationToken);

        var delay = await db.AppSettings.AsNoTracking().Select(s => s.ReminderDelayMinutes).SingleAsync(cancellationToken);
        var now = Now;
        var segment = new TripSegment { Seq = 1, Kind = SegmentKind.Start, StaffId = staffId, VehicleId = vehicle.Id, StartedAtUtc = now };
        var trip = new Trip
        {
            OriginLotId = vehicle.CurrentLotId!.Value,
            DestinationLocationId = request.DestinationLocationId,
            CurrentStaffId = staffId,
            CurrentVehicleId = vehicle.Id,
            StartedAtUtc = now,
            NextReminderAtUtc = now.AddMinutes(delay),
            Segments = [segment],
        };
        vehicle.Status = VehicleStatus.Active;
        vehicle.CurrentLotId = null;
        db.Trips.Add(trip);

        // The signature file only gets its final name once the trip is committed (D-034).
        await fileStore.SaveAsync(FileKind.Signature, new MemoryStream(signature), async name =>
        {
            segment.SignatureFileName = name;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }, cancellationToken);

        return await queries.GetDetailAsync(trip.Id, staffId, viewerIsAdmin: false, null, cancellationToken);
    }

    /// <summary>Changes the vehicle mid-trip; the same trip continues in a new segment (TRP-07, D-012).</summary>
    public async Task<TripDetailResponse> SwapVehicleAsync(int staffId, int tripId, SwapVehicleRequest request, CancellationToken cancellationToken)
    {
        var current = await LoadOwnActiveTripAsync(staffId, tripId, cancellationToken);
        var identified = await FindVehicleAsync(request.NewVehicleQrToken, request.NewVehicleRegistration, tracked: false, cancellationToken);
        if (identified.Id == current.CurrentVehicleId)
        {
            throw BadRequestException.ForField("newVehicleQrToken", "This is already the vehicle on this trip.");
        }

        if (request.OldVehicleLotId is { } oldLotId)
        {
            await EnsureActiveLotAsync(oldLotId, "oldVehicleLotId", cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Staff(staffId), AppLocks.Trip(tripId), AppLocks.Vehicle(current.CurrentVehicleId), AppLocks.Vehicle(identified.Id));

        var trip = await LoadOwnActiveTripAsync(staffId, tripId, cancellationToken, tracked: true);
        var newVehicle = await LoadVehicleAsync(identified.Id, cancellationToken);
        ThrowIfBlocked(StartBlock(newVehicle));
        ThrowIfBlocked(await TransferBlockAsync(newVehicle, cancellationToken));
        var oldVehicle = await LoadVehicleAsync(trip.CurrentVehicleId, cancellationToken);

        var now = Now;
        CloseOpenSegment(trip, now);
        trip.Segments.Add(new TripSegment
        {
            Seq = trip.Segments.Max(s => s.Seq) + 1,
            Kind = SegmentKind.VehicleSwap,
            StaffId = staffId,
            VehicleId = newVehicle.Id,
            StartedAtUtc = now,
            Reason = Text.Clean(request.Reason),
            OutgoingVehicleLotId = request.OldVehicleLotId,
        });

        // The swapped-out vehicle is Available where staff left it; admins decide its status (D-043).
        oldVehicle.Status = VehicleStatus.Available;
        oldVehicle.CurrentLotId = request.OldVehicleLotId;
        newVehicle.Status = VehicleStatus.Active;
        newVehicle.CurrentLotId = null;
        trip.CurrentVehicleId = newVehicle.Id;

        var placeName = await PlaceNameAsync(request.OldVehicleLotId, cancellationToken);
        var released = notifications.Add(
            await notifications.ActiveAdminIdsAsync(cancellationToken),
            NotificationType.VehicleReleased,
            $"Vehicle swapped out: {oldVehicle.RegistrationNumber}",
            $"{oldVehicle.RegistrationNumber} was swapped out of trip {trip.Id} ({Text.Clean(request.Reason)}) and left {placeName}. Check its status.",
            "Trip",
            trip.Id);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await notifications.PushAsync(released, cancellationToken);
        return await queries.GetDetailAsync(tripId, staffId, viewerIsAdmin: false, null, cancellationToken);
    }

    /// <summary>Hands the trip to another staff member, who becomes its driver from now on (TRP-07, D-020, D-041).</summary>
    public async Task<TripDetailResponse> HandOverAsync(int staffId, int tripId, HandOverRequest request, CancellationToken cancellationToken)
    {
        await LoadOwnActiveTripAsync(staffId, tripId, cancellationToken);
        if (request.ToStaffId == staffId)
        {
            throw HandoverTarget("You can't hand a trip over to yourself.");
        }

        var target = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == request.ToStaffId, cancellationToken);
        if (target is null || target.Role != UserRole.Staff || !target.IsActive)
        {
            throw HandoverTarget("Pick an active staff member to hand the trip over to.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Staff(staffId), AppLocks.Staff(target.Id), AppLocks.Trip(tripId));

        var trip = await LoadOwnActiveTripAsync(staffId, tripId, cancellationToken, tracked: true);
        if (await ActiveTripBlockAsync(target.Id, cancellationToken) is not null)
        {
            throw HandoverTarget($"{target.FullName} is already driving another vehicle.");
        }

        var now = Now;
        CloseOpenSegment(trip, now);
        trip.Segments.Add(new TripSegment
        {
            Seq = trip.Segments.Max(s => s.Seq) + 1,
            Kind = SegmentKind.HandOver,
            StaffId = target.Id,
            VehicleId = trip.CurrentVehicleId,
            StartedAtUtc = now,
            Reason = Text.Clean(request.Reason),
        });
        trip.CurrentStaffId = target.Id;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await queries.GetDetailAsync(tripId, staffId, viewerIsAdmin: false, null, cancellationToken);
    }

    /// <summary>Cancels the trip now; its hours still count (TRP-08, BR-07). The vehicle is left where staff say (D-043).</summary>
    public async Task<TripDetailResponse> CancelAsync(int staffId, int tripId, CancelTripRequest request, CancellationToken cancellationToken)
    {
        var current = await LoadOwnActiveTripAsync(staffId, tripId, cancellationToken);
        if (request.VehicleLotId is { } lotId)
        {
            await EnsureActiveLotAsync(lotId, "vehicleLotId", cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Staff(staffId), AppLocks.Trip(tripId), AppLocks.Vehicle(current.CurrentVehicleId));

        var trip = await LoadOwnActiveTripAsync(staffId, tripId, cancellationToken, tracked: true);
        var vehicle = await LoadVehicleAsync(trip.CurrentVehicleId, cancellationToken);
        var now = Now;
        CloseOpenSegment(trip, now);
        trip.Status = TripStatus.Cancelled;
        trip.EndedAtUtc = now;
        trip.ReturnLotId = request.VehicleLotId;
        trip.CancelCategory = request.Category;
        trip.CancelNote = Text.Clean(request.Note);
        trip.NextReminderAtUtc = null;
        vehicle.Status = VehicleStatus.Available;
        vehicle.CurrentLotId = request.VehicleLotId;

        var placeName = await PlaceNameAsync(request.VehicleLotId, cancellationToken);
        var cancelled = notifications.Add(
            await notifications.ActiveAdminIdsAsync(cancellationToken),
            NotificationType.TripCancelled,
            $"Trip cancelled: {vehicle.RegistrationNumber}",
            $"Trip {trip.Id} was cancelled ({request.Category}: {trip.CancelNote}). {vehicle.RegistrationNumber} was left {placeName}. Check its status.",
            "Trip",
            trip.Id);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await notifications.PushAsync(cancelled, cancellationToken);
        return await queries.GetDetailAsync(tripId, staffId, viewerIsAdmin: false, null, cancellationToken);
    }

    /// <summary>
    /// Ends the trip at any active lot after the vehicle is confirmed by scan or registration (TRP-09).
    /// A full lot gives a warning, never a block (BR-09).
    /// </summary>
    public async Task<TripDetailResponse> EndAsync(int staffId, int tripId, EndTripRequest request, CancellationToken cancellationToken)
    {
        var current = await LoadOwnActiveTripAsync(staffId, tripId, cancellationToken);
        var scanned = await FindVehicleAsync(request.VehicleQrToken, request.VehicleRegistration, tracked: false, cancellationToken);
        if (scanned.Id != current.CurrentVehicleId)
        {
            throw new ConflictException(
                ErrorCodes.VehicleMismatch,
                "Different vehicle",
                $"You identified {scanned.RegistrationNumber}, but this trip's vehicle is {current.CurrentVehicle.RegistrationNumber}.");
        }

        var returnLot = await EnsureActiveLotAsync(request.ReturnLotId, "returnLotId", cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Staff(staffId), AppLocks.Trip(tripId), AppLocks.Vehicle(current.CurrentVehicleId));

        var trip = await LoadOwnActiveTripAsync(staffId, tripId, cancellationToken, tracked: true);
        var vehicle = await LoadVehicleAsync(trip.CurrentVehicleId, cancellationToken);
        var now = Now;
        CloseOpenSegment(trip, now);
        trip.Status = TripStatus.Ended;
        trip.EndedAtUtc = now;
        trip.ReturnLotId = returnLot.Id;
        trip.NextReminderAtUtc = null;
        vehicle.Status = VehicleStatus.Available;
        vehicle.CurrentLotId = returnLot.Id;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var warnings = await CapacityWarningsAsync(returnLot.Id, vehicle.VehicleTypeId, cancellationToken);
        return await queries.GetDetailAsync(tripId, staffId, viewerIsAdmin: false, warnings, cancellationToken);
    }

    // ----- Rules -----

    /// <summary>Why a vehicle can't leave a lot right now (BR-04), or null.</summary>
    private static Notice? StartBlock(Vehicle vehicle) => vehicle switch
    {
        { Status: VehicleStatus.Active } => new Notice(ErrorCodes.VehicleNotAvailable, $"{vehicle.RegistrationNumber} is already out on a trip or transfer."),
        { Status: VehicleStatus.UnderMaintenance } => new Notice(ErrorCodes.VehicleNotAvailable, $"{vehicle.RegistrationNumber} is under maintenance."),
        { Status: VehicleStatus.Inactive } => new Notice(ErrorCodes.VehicleNotAvailable, $"{vehicle.RegistrationNumber} is inactive."),
        { CurrentLot: null } => new Notice(ErrorCodes.VehicleNotAtLot, $"{vehicle.RegistrationNumber} isn't recorded at any parking lot. Ask an admin to place it."),
        { CurrentLot.IsActive: false } => new Notice(ErrorCodes.LotInactive, $"{vehicle.CurrentLot.Name} is inactive."),
        _ => null,
    };

    /// <summary>One vehicle at a time per staff member: an active trip or a transfer being driven (BR-02, D-026).</summary>
    private async Task<Notice?> ActiveTripBlockAsync(int staffId, CancellationToken cancellationToken) =>
        await db.IsStaffBusyAsync(staffId, cancellationToken)
            ? new Notice(ErrorCodes.StaffHasActiveTrip, "There is already an active trip or a transfer being driven. Finish it first.")
            : null;

    /// <summary>A vehicle with a pending or in-transit transfer can't start a trip.</summary>
    private async Task<Notice?> TransferBlockAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
        await db.HasOpenTransferAsync(vehicle.Id, cancellationToken)
            ? new Notice(ErrorCodes.VehicleInTransfer, $"{vehicle.RegistrationNumber} is waiting for or on a transfer.")
            : null;

    private async Task<string> PlaceNameAsync(int? lotId, CancellationToken cancellationToken) =>
        lotId is { } id
            ? "at " + await db.ParkingLots.Where(l => l.Id == id).Select(l => l.Name).SingleAsync(cancellationToken)
            : "not at any lot";

    private static void ThrowIfBlocked(Notice? problem)
    {
        if (problem is not null)
        {
            throw new ConflictException(problem.Code, "Can't start this trip", problem.Message);
        }
    }

    private async Task<IReadOnlyList<Notice>> CapacityWarningsAsync(int lotId, int vehicleTypeId, CancellationToken cancellationToken)
    {
        var lot = await lots.GetAsync(lotId, includeInactive: true, cancellationToken);
        var typeRow = lot.ByType.SingleOrDefault(t => t.VehicleTypeId == vehicleTypeId);
        if (lot.Total.IsOverCapacity)
        {
            return [new Notice(ErrorCodes.LotFull, $"{lot.Name} is over capacity ({lot.Total.Occupied} of {lot.Total.Capacity}). The vehicle was still returned.")];
        }

        return typeRow is { Available: < 0 }
            ? [new Notice(ErrorCodes.LotFull, $"{lot.Name} has no free {typeRow.Type} spaces ({typeRow.Occupied} of {typeRow.Capacity}). The vehicle was still returned.")]
            : [];
    }

    // ----- Loading and locking -----

    private async Task<Trip> LoadOwnActiveTripAsync(int staffId, int tripId, CancellationToken cancellationToken, bool tracked = false)
    {
        var query = db.Trips.Include(t => t.Segments).Include(t => t.CurrentVehicle).AsQueryable();
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        var trip = await query.SingleOrDefaultAsync(t => t.Id == tripId, cancellationToken);
        if (trip is null || (trip.CurrentStaffId != staffId && trip.Segments.All(s => s.StaffId != staffId)))
        {
            throw new NotFoundException("Trip not found.");
        }

        if (trip.Status != TripStatus.Active)
        {
            throw new ConflictException(ErrorCodes.TripNotActive, "Trip already closed", "This trip has already ended or been cancelled.");
        }

        if (trip.CurrentStaffId != staffId)
        {
            throw new ForbiddenException("This trip was handed over to someone else.", ErrorCodes.NotYourTrip);
        }

        return trip;
    }

    private async Task<Vehicle> FindVehicleAsync(string? qrToken, string? registration, bool tracked, CancellationToken cancellationToken)
    {
        var vehicles = db.Vehicles.Include(v => v.VehicleType).ThenInclude(t => t.Category).Include(v => v.CurrentLot).AsQueryable();
        if (!tracked)
        {
            vehicles = vehicles.AsNoTracking();
        }

        Vehicle? vehicle;
        if (!string.IsNullOrWhiteSpace(qrToken))
        {
            var token = qrToken.Trim();
            vehicle = await vehicles.SingleOrDefaultAsync(v => v.QrToken == token, cancellationToken);
        }
        else
        {
            var key = VehicleService.RegistrationKey(registration ?? string.Empty);
            vehicle = key.Length == 0 ? null : await vehicles.SingleOrDefaultAsync(v => v.RegistrationKey == key, cancellationToken);
        }

        return vehicle ?? throw new NotFoundException(
            !string.IsNullOrWhiteSpace(qrToken) ? "This QR code doesn't belong to any vehicle." : "No vehicle has this registration number.",
            ErrorCodes.VehicleNotFound);
    }

    private Task<Vehicle> LoadVehicleAsync(int vehicleId, CancellationToken cancellationToken) =>
        db.Vehicles.Include(v => v.VehicleType).ThenInclude(t => t.Category).Include(v => v.CurrentLot)
            .SingleAsync(v => v.Id == vehicleId, cancellationToken);

    private async Task EnsureDestinationAsync(int locationId, CancellationToken cancellationToken)
    {
        var usable = await db.Locations.AsNoTracking()
            .Where(l => l.Id == locationId)
            .Select(l => (bool?)(l.IsActive && l.Area.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        if (usable != true)
        {
            throw BadRequestException.ForField("destinationLocationId", usable is null ? "This destination doesn't exist." : "This destination is no longer in use.");
        }
    }

    private async Task<ParkingLot> EnsureActiveLotAsync(int lotId, string field, CancellationToken cancellationToken)
    {
        var lot = await db.ParkingLots.AsNoTracking().SingleOrDefaultAsync(l => l.Id == lotId, cancellationToken)
            ?? throw BadRequestException.ForField(field, "This parking lot doesn't exist.");
        if (!lot.IsActive)
        {
            throw new ConflictException(ErrorCodes.LotInactive, "Lot inactive", $"{lot.Name} is inactive and can't be used (LOT-07).");
        }

        return lot;
    }

    private static void CloseOpenSegment(Trip trip, DateTime now) =>
        trip.Segments.Single(s => s.EndedAtUtc is null).EndedAtUtc = now;

    private static ConflictException HandoverTarget(string message) =>
        new(ErrorCodes.HandoverTargetInvalid, "Can't hand over", message);

    /// <summary>Decodes the base64 signature; the file store then checks it is a real PNG within 512 KB.</summary>
    private static byte[] DecodeSignature(string base64)
    {
        var data = base64.Trim();
        var comma = data.IndexOf(',', StringComparison.Ordinal);
        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
        {
            data = data[(comma + 1)..];
        }

        try
        {
            return Convert.FromBase64String(data);
        }
        catch (FormatException)
        {
            throw new BadRequestException(
                ErrorCodes.InvalidFile,
                "The signature could not be read. Sign again.",
                new Dictionary<string, string[]> { ["signaturePngBase64"] = ["The signature could not be read. Sign again."] });
        }
    }
}
