using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Features.Trips;

/// <summary>Rules shared by trips and transfers (BR-02, D-026).</summary>
public static class DutyRules
{
    /// <summary>
    /// A staff member is busy while they have an active trip or drive an in-transit transfer:
    /// one person drives one vehicle at a time (BR-02, D-026).
    /// </summary>
    public static async Task<bool> IsStaffBusyAsync(this AppDbContext db, int staffId, CancellationToken cancellationToken) =>
        await db.Trips.AnyAsync(t => t.Status == TripStatus.Active && t.CurrentStaffId == staffId, cancellationToken)
        || await db.Transfers.AnyAsync(t => t.Status == TransferStatus.InTransit && t.DepartedByUserId == staffId, cancellationToken);

    /// <summary>The vehicle has a pending or in-transit transfer, so it can't start a trip or be moved by hand.</summary>
    public static Task<bool> HasOpenTransferAsync(this AppDbContext db, int vehicleId, CancellationToken cancellationToken) =>
        db.Transfers.AnyAsync(
            t => t.VehicleId == vehicleId && (t.Status == TransferStatus.PendingDeparture || t.Status == TransferStatus.InTransit),
            cancellationToken);
}
