using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Features.Lots.Dtos;

namespace VehicleManagementDM.Api.Features.Lots;

/// <summary>Parking lots, capacities, baselines and occupancy (LOT-01..07, BR-05, DATABASE_DESIGN.md §4).</summary>
public sealed class LotService(AppDbContext db)
{
    public async Task<IReadOnlyList<LotResponse>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var lots = await LotsWithCapacities()
            .Where(l => includeInactive || l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
        return await ToResponsesAsync(lots, cancellationToken);
    }

    public async Task<LotResponse> GetAsync(int id, bool includeInactive, CancellationToken cancellationToken)
    {
        var lot = await LotsWithCapacities().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null || (!lot.IsActive && !includeInactive))
        {
            throw new NotFoundException("Parking lot not found.");
        }

        return (await ToResponsesAsync([lot], cancellationToken))[0];
    }

    public async Task<LotResponse> CreateAsync(LotRequest request, CancellationToken cancellationToken)
    {
        var lot = new ParkingLot();
        await ApplyAsync(lot, request, cancellationToken);
        db.ParkingLots.Add(lot);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(lot.Id, includeInactive: true, cancellationToken);
    }

    public async Task<LotResponse> UpdateAsync(int id, UpdateLotRequest request, CancellationToken cancellationToken)
    {
        var lot = await db.ParkingLots.Include(l => l.TypeCapacities).SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Parking lot not found.");
        db.ExpectRowVersion(lot, request.RowVersion);
        await ApplyAsync(lot, request, cancellationToken);
        db.Entry(lot).State = EntityState.Modified; // checks rowVersion even when only per-type rows changed
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(lot.Id, includeInactive: true, cancellationToken);
    }

    /// <summary>Inactive lots can't be used to start, return or receive transfers (LOT-07).</summary>
    public async Task<LotResponse> SetStatusAsync(int id, LotStatusRequest request, CancellationToken cancellationToken)
    {
        var lot = await db.ParkingLots.SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Parking lot not found.");
        db.ExpectRowVersion(lot, request.RowVersion);
        lot.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(lot.Id, includeInactive: true, cancellationToken);
    }

    private IQueryable<ParkingLot> LotsWithCapacities() =>
        db.ParkingLots
            .AsNoTracking()
            .Include(l => l.TypeCapacities)
            .ThenInclude(c => c.VehicleType)
            .ThenInclude(t => t.Category);

    private async Task ApplyAsync(ParkingLot lot, LotRequest request, CancellationToken cancellationToken)
    {
        var typeIds = request.TypeCapacities.Select(c => c.VehicleTypeId).ToList();
        if (typeIds.Count != typeIds.Distinct().Count())
        {
            throw BadRequestException.ForField("typeCapacities", "Each vehicle type can appear only once.");
        }

        var knownTypes = await db.VehicleTypes.Where(t => typeIds.Contains(t.Id)).Select(t => t.Id).ToListAsync(cancellationToken);
        if (knownTypes.Count != typeIds.Count)
        {
            throw BadRequestException.ForField("typeCapacities", "One of the vehicle types doesn't exist.");
        }

        lot.Code = request.Code.Trim().ToUpperInvariant();
        lot.Name = Text.Clean(request.Name);
        lot.Address = Text.Clean(request.Address);
        lot.ContactName = Text.Clean(request.ContactName);
        lot.ContactNumber = request.ContactNumber.Trim();
        lot.OpensAt = TimeOfDay.Parse(request.OpensAt);
        lot.ClosesAt = TimeOfDay.Parse(request.ClosesAt);
        lot.LotType = request.LotType!.Value;
        lot.TotalCapacity = request.TotalCapacity!.Value;
        lot.BaselineOccupied = request.BaselineOccupied;

        // Replace the per-type rows with exactly what the request lists.
        lot.TypeCapacities.RemoveAll(existing => !typeIds.Contains(existing.VehicleTypeId));
        foreach (var item in request.TypeCapacities)
        {
            var row = lot.TypeCapacities.SingleOrDefault(c => c.VehicleTypeId == item.VehicleTypeId);
            if (row is null)
            {
                row = new LotTypeCapacity { VehicleTypeId = item.VehicleTypeId };
                lot.TypeCapacities.Add(row);
            }

            row.Capacity = item.Capacity;
            row.BaselineOccupied = item.BaselineOccupied;
        }
    }

    /// <summary>
    /// Occupied = baseline + vehicles whose current lot is this lot (per type and in total, BR-05).
    /// That count includes parked inactive/maintenance vehicles and vehicles in a transfer that hasn't arrived.
    /// </summary>
    private async Task<IReadOnlyList<LotResponse>> ToResponsesAsync(IReadOnlyList<ParkingLot> lots, CancellationToken cancellationToken)
    {
        var lotIds = lots.Select(l => l.Id).ToList();
        var counts = await db.Vehicles
            .AsNoTracking()
            .Where(v => v.CurrentLotId != null && lotIds.Contains(v.CurrentLotId.Value))
            .GroupBy(v => new { LotId = v.CurrentLotId!.Value, v.VehicleTypeId })
            .Select(g => new { g.Key.LotId, g.Key.VehicleTypeId, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var typeIdsWithoutCapacity = counts
            .Where(c => !lots.Single(l => l.Id == c.LotId).TypeCapacities.Any(t => t.VehicleTypeId == c.VehicleTypeId))
            .Select(c => c.VehicleTypeId)
            .Distinct()
            .ToList();
        var extraTypes = await db.VehicleTypes
            .AsNoTracking()
            .Where(t => typeIdsWithoutCapacity.Contains(t.Id))
            .Select(t => new { t.Id, t.Name, Category = t.Category.Name })
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return lots.Select(lot =>
        {
            var lotCounts = counts.Where(c => c.LotId == lot.Id).ToDictionary(c => c.VehicleTypeId, c => c.Count);

            var byType = lot.TypeCapacities
                .Select(c =>
                {
                    var occupied = c.BaselineOccupied + lotCounts.GetValueOrDefault(c.VehicleTypeId);
                    return new OccupancyByType(c.VehicleTypeId, c.VehicleType.Name, c.VehicleType.Category.Name, c.Capacity, c.BaselineOccupied, occupied, c.Capacity - occupied);
                })
                .Concat(lotCounts
                    .Where(pair => lot.TypeCapacities.All(c => c.VehicleTypeId != pair.Key))
                    .Select(pair => new OccupancyByType(pair.Key, extraTypes[pair.Key].Name, extraTypes[pair.Key].Category, null, 0, pair.Value, null)))
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Type)
                .ToList();

            var occupiedTotal = lot.BaselineOccupied + lotCounts.Values.Sum();
            var available = lot.TotalCapacity - occupiedTotal;
            return new LotResponse(
                lot.Id,
                lot.Code,
                lot.Name,
                lot.Address,
                lot.ContactName,
                lot.ContactNumber,
                TimeOfDay.Format(lot.OpensAt),
                TimeOfDay.Format(lot.ClosesAt),
                lot.LotType,
                lot.IsActive,
                lot.BaselineOccupied,
                new OccupancyTotal(lot.TotalCapacity, occupiedTotal, available, available <= 0, available < 0),
                byType,
                lot.RowVersion);
        }).ToList();
    }
}
