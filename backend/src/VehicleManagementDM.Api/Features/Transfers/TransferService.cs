using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Database;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Common.Time;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Notifications;
using VehicleManagementDM.Api.Features.Trips;
using VehicleManagementDM.Api.Features.Trips.Dtos;

namespace VehicleManagementDM.Api.Features.Transfers;

/// <summary>
/// Transfers between lots (TRF-01..07). Admins create and cancel; any staff member confirms departure
/// (and drives) or arrival (D-020, D-047). The vehicle counts at the origin lot until it arrives (TRF-03).
/// </summary>
public sealed class TransferService(
    AppDbContext db,
    LotService lots,
    NotificationService notifications,
    TimeProvider timeProvider)
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<TransferResponse> CreateAsync(CreateTransferRequest request, int adminId, CancellationToken cancellationToken)
    {
        var toLot = await db.ParkingLots.AsNoTracking().SingleOrDefaultAsync(l => l.Id == request.ToLotId, cancellationToken)
            ?? throw BadRequestException.ForField("toLotId", "This parking lot doesn't exist.");
        if (!toLot.IsActive)
        {
            throw new ConflictException(ErrorCodes.LotInactive, "Lot inactive", $"{toLot.Name} is inactive and can't receive transfers (LOT-07).");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Vehicle(request.VehicleId));

        var vehicle = await db.Vehicles.Include(v => v.CurrentLot).SingleOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.", ErrorCodes.VehicleNotFound);
        if (vehicle.Status != VehicleStatus.Available)
        {
            throw new ConflictException(
                ErrorCodes.VehicleNotAvailable,
                "Vehicle not available",
                $"{vehicle.RegistrationNumber} is {(vehicle.Status == VehicleStatus.Active ? "out on a trip or transfer" : vehicle.Status == VehicleStatus.UnderMaintenance ? "under maintenance" : "inactive")} and can't be transferred (VEH-03).");
        }

        if (vehicle.CurrentLotId is not { } fromLotId)
        {
            throw new ConflictException(ErrorCodes.VehicleNotAtLot, "Vehicle not at a lot", $"{vehicle.RegistrationNumber} isn't recorded at any lot. Place it first.");
        }

        if (fromLotId == toLot.Id)
        {
            throw BadRequestException.ForField("toLotId", $"{vehicle.RegistrationNumber} is already at {toLot.Name}.");
        }

        if (await db.HasOpenTransferAsync(vehicle.Id, cancellationToken))
        {
            throw new ConflictException(ErrorCodes.VehicleInTransfer, "Vehicle in transfer", $"{vehicle.RegistrationNumber} already has an open transfer.");
        }

        var transfer = new Transfer
        {
            VehicleId = vehicle.Id,
            FromLotId = fromLotId,
            ToLotId = toLot.Id,
            Note = Text.CleanOptional(request.Note),
            CreatedByUserId = adminId,
            CreatedAtUtc = Now,
        };
        db.Transfers.Add(transfer);
        await db.SaveChangesAsync(cancellationToken);

        var notices = notifications.Add(
            await notifications.ActiveStaffIdsAsync(cancellationToken),
            NotificationType.TransferAwaitingDeparture,
            $"Transfer waiting: {vehicle.RegistrationNumber}",
            $"Move {vehicle.RegistrationNumber} from {vehicle.CurrentLot!.Name} to {toLot.Name}. Whoever confirms departure drives it.",
            "Transfer",
            transfer.Id);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await notifications.PushAsync(notices, cancellationToken);
        return await GetAsync(transfer.Id, null, cancellationToken);
    }

    /// <summary>Admins can cancel only before departure (TRF-06).</summary>
    public async Task<TransferResponse> CancelAsync(int id, int adminId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Transfer(id));
        var transfer = await FindAsync(id, cancellationToken);
        RequireStatus(transfer, TransferStatus.PendingDeparture, "Only a transfer that hasn't departed can be cancelled.");

        transfer.Status = TransferStatus.Cancelled;
        transfer.CancelledByUserId = adminId;
        transfer.CancelledAtUtc = Now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, null, cancellationToken);
    }

    /// <summary>
    /// A staff member confirms departure and becomes the driver (TRF-02, D-020). They must not be on a trip
    /// or driving another transfer (D-026). The vehicle becomes Active but still counts at the origin (TRF-03).
    /// </summary>
    public async Task<TransferResponse> DepartAsync(int id, int staffId, CancellationToken cancellationToken)
    {
        var vehicleId = await db.Transfers.Where(t => t.Id == id).Select(t => (int?)t.VehicleId).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Transfer not found.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Transfer(id), AppLocks.Staff(staffId), AppLocks.Vehicle(vehicleId));

        var transfer = await FindAsync(id, cancellationToken);
        RequireStatus(transfer, TransferStatus.PendingDeparture, "This transfer has already departed, arrived or been cancelled.");
        if (await db.IsStaffBusyAsync(staffId, cancellationToken))
        {
            throw new ConflictException(ErrorCodes.StaffHasActiveTrip, "Already driving", "You have an active trip or are driving another transfer. Finish it first (D-026).");
        }

        var vehicle = transfer.Vehicle;
        if (vehicle.Status != VehicleStatus.Available)
        {
            throw new ConflictException(ErrorCodes.VehicleNotAvailable, "Vehicle not available", $"{vehicle.RegistrationNumber} is no longer available. Ask an admin.");
        }

        if (vehicle.CurrentLotId != transfer.FromLotId)
        {
            throw new ConflictException(ErrorCodes.VehicleNotAtLot, "Vehicle moved", $"{vehicle.RegistrationNumber} is no longer at {transfer.FromLot.Name}. Ask an admin.");
        }

        var delay = await db.AppSettings.AsNoTracking().Select(s => s.ReminderDelayMinutes).SingleAsync(cancellationToken);
        var now = Now;
        transfer.Status = TransferStatus.InTransit;
        transfer.DepartedByUserId = staffId;
        transfer.DepartedAtUtc = now;
        transfer.NextReminderAtUtc = now.AddMinutes(delay);
        vehicle.Status = VehicleStatus.Active; // CurrentLotId stays the origin until arrival (TRF-03)

        var notices = notifications.Add(
            (await notifications.ActiveStaffIdsAsync(cancellationToken)).Where(userId => userId != staffId),
            NotificationType.TransferAwaitingArrival,
            $"Transfer on the way: {vehicle.RegistrationNumber}",
            $"{vehicle.RegistrationNumber} left {transfer.FromLot.Name} for {transfer.ToLot.Name}. Confirm arrival when it gets there.",
            "Transfer",
            transfer.Id);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await notifications.PushAsync(notices, cancellationToken);
        return await GetAsync(id, null, cancellationToken);
    }

    /// <summary>Any staff member confirms arrival (TRF-04). A full lot gives a warning, never a block (BR-09).</summary>
    public async Task<TransferResponse> ArriveAsync(int id, int staffId, CancellationToken cancellationToken)
    {
        var vehicleId = await db.Transfers.Where(t => t.Id == id).Select(t => (int?)t.VehicleId).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Transfer not found.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockAsync(cancellationToken, AppLocks.Transfer(id), AppLocks.Vehicle(vehicleId));

        var transfer = await FindAsync(id, cancellationToken);
        RequireStatus(transfer, TransferStatus.InTransit, "Only a transfer on the road can be confirmed as arrived.");

        transfer.Status = TransferStatus.Completed;
        transfer.ArrivedByUserId = staffId;
        transfer.ArrivedAtUtc = Now;
        transfer.NextReminderAtUtc = null;
        transfer.Vehicle.Status = VehicleStatus.Available;
        transfer.Vehicle.CurrentLotId = transfer.ToLotId;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var warnings = await CapacityWarningsAsync(transfer.ToLotId, transfer.Vehicle.VehicleTypeId, cancellationToken);
        return await GetAsync(id, warnings, cancellationToken);
    }

    public async Task<TransferResponse> GetAsync(int id, IReadOnlyList<Notice>? warnings, CancellationToken cancellationToken)
    {
        var transfer = await WithDetails(db.Transfers.AsNoTracking()).SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Transfer not found.");
        return ToResponse(transfer, Now, warnings);
    }

    public async Task<PagedResult<TransferResponse>> ListAsync(TransferQuery query, CancellationToken cancellationToken)
    {
        var transfers = db.Transfers.AsNoTracking();
        if (query.Status is { } status)
        {
            transfers = transfers.Where(t => t.Status == status);
        }

        if (query.LotId is { } lotId)
        {
            transfers = transfers.Where(t => t.FromLotId == lotId || t.ToLotId == lotId);
        }

        if (query.VehicleId is { } vehicleId)
        {
            transfers = transfers.Where(t => t.VehicleId == vehicleId);
        }

        if (query.From is { } from)
        {
            var fromUtc = Hours.GstDayStartUtc(from);
            transfers = transfers.Where(t => t.CreatedAtUtc >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = Hours.GstDayStartUtc(to.AddDays(1));
            transfers = transfers.Where(t => t.CreatedAtUtc < toUtc);
        }

        var ordered = WithDetails(transfers).OrderByDescending(t => t.CreatedAtUtc).ThenByDescending(t => t.Id);
        var total = await ordered.CountAsync(cancellationToken);
        var page = await ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        var now = Now;
        return new PagedResult<TransferResponse>(page.Select(t => ToResponse(t, now, null)).ToList(), query.Page, query.PageSize, total);
    }

    /// <summary>Open transfers any staff member can act on (S-22, D-047).</summary>
    public async Task<TransferTasksResponse> TasksAsync(CancellationToken cancellationToken)
    {
        var open = await WithDetails(db.Transfers.AsNoTracking())
            .Where(t => t.Status == TransferStatus.PendingDeparture || t.Status == TransferStatus.InTransit)
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var now = Now;
        return new TransferTasksResponse(
            open.Where(t => t.Status == TransferStatus.PendingDeparture).Select(t => ToResponse(t, now, null)).ToList(),
            open.Where(t => t.Status == TransferStatus.InTransit).Select(t => ToResponse(t, now, null)).ToList());
    }

    /// <summary>The open transfer of a scanned vehicle, e.g. to confirm arrival at the destination.</summary>
    public async Task<TransferResponse> ByVehicleQrAsync(string qrToken, CancellationToken cancellationToken)
    {
        var token = qrToken.Trim();
        var vehicleId = await db.Vehicles.Where(v => v.QrToken == token).Select(v => (int?)v.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("This QR code doesn't belong to any vehicle.", ErrorCodes.VehicleNotFound);
        var transferId = await db.Transfers
            .Where(t => t.VehicleId == vehicleId && (t.Status == TransferStatus.PendingDeparture || t.Status == TransferStatus.InTransit))
            .Select(t => (int?)t.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("This vehicle has no open transfer.");
        return await GetAsync(transferId, null, cancellationToken);
    }

    private async Task<Transfer> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Transfers
            .Include(t => t.Vehicle)
            .Include(t => t.FromLot)
            .Include(t => t.ToLot)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
        ?? throw new NotFoundException("Transfer not found.");

    private static void RequireStatus(Transfer transfer, TransferStatus expected, string message)
    {
        if (transfer.Status != expected)
        {
            throw new ConflictException(ErrorCodes.TransferInvalidState, "Not possible now", message);
        }
    }

    private async Task<IReadOnlyList<Notice>> CapacityWarningsAsync(int lotId, int vehicleTypeId, CancellationToken cancellationToken)
    {
        var lot = await lots.GetAsync(lotId, includeInactive: true, cancellationToken);
        if (lot.Total.IsOverCapacity)
        {
            return [new Notice(ErrorCodes.LotFull, $"{lot.Name} is over capacity ({lot.Total.Occupied} of {lot.Total.Capacity}). The vehicle was still received.")];
        }

        var typeRow = lot.ByType.SingleOrDefault(t => t.VehicleTypeId == vehicleTypeId);
        return typeRow is { Available: < 0 }
            ? [new Notice(ErrorCodes.LotFull, $"{lot.Name} has no free {typeRow.Type} spaces ({typeRow.Occupied} of {typeRow.Capacity}). The vehicle was still received.")]
            : [];
    }

    private static IQueryable<Transfer> WithDetails(IQueryable<Transfer> transfers) =>
        transfers
            .Include(t => t.Vehicle).ThenInclude(v => v.VehicleType).ThenInclude(vt => vt.Category)
            .Include(t => t.FromLot)
            .Include(t => t.ToLot)
            .Include(t => t.CreatedBy)
            .Include(t => t.DepartedBy)
            .Include(t => t.ArrivedBy)
            .Include(t => t.CancelledBy);

    private static TransferResponse ToResponse(Transfer t, DateTime now, IReadOnlyList<Notice>? warnings) => new(
        t.Id,
        t.Status,
        new VehicleRef(t.Vehicle.Id, t.Vehicle.RegistrationNumber, t.Vehicle.VehicleType.Category.Name, t.Vehicle.VehicleType.Name),
        new LotRef(t.FromLot.Id, t.FromLot.Code, t.FromLot.Name),
        new LotRef(t.ToLot.Id, t.ToLot.Code, t.ToLot.Name),
        t.Note,
        Person(t.CreatedBy),
        t.CreatedAtUtc,
        t.DepartedBy is null ? null : Person(t.DepartedBy),
        t.DepartedAtUtc,
        t.ArrivedBy is null ? null : Person(t.ArrivedBy),
        t.ArrivedAtUtc,
        t.CancelledBy is null ? null : Person(t.CancelledBy),
        t.CancelledAtUtc,
        t.DepartedAtUtc is { } departed ? (long)((t.ArrivedAtUtc ?? now) - departed).TotalSeconds : null,
        t.Status == TransferStatus.InTransit && (t.ReminderCount > 0 || t.NextReminderAtUtc <= now),
        warnings ?? []);

    private static StaffRef Person(User user) =>
        new(user.Id, user.LoginId, user.FullName, user.PhotoFileName is null ? null : FileStore.Url(FileKind.StaffPhoto, user.PhotoFileName));
}
