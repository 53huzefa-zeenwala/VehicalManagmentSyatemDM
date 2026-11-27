using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Features.Vehicles;

/// <summary>
/// One vehicle's history, newest first (VEH-05): its trip segments, its transfers, and admin changes to it
/// (status, location, details) from the audit log. Moved from M2 to M4 so it is complete (D-045).
/// </summary>
public sealed class VehicleHistoryService(AppDbContext db, TimeProvider timeProvider)
{
    public async Task<PagedResult<VehicleHistoryEntry>> HistoryAsync(int vehicleId, PageQuery page, CancellationToken cancellationToken)
    {
        if (!await db.Vehicles.AnyAsync(v => v.Id == vehicleId, cancellationToken))
        {
            throw new NotFoundException("Vehicle not found.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var segments = await db.TripSegments.AsNoTracking()
            .Where(s => s.VehicleId == vehicleId)
            .Select(s => new { s.TripId, s.Kind, s.StartedAtUtc, s.EndedAtUtc, Staff = s.Staff.FullName, s.Trip.Status, Origin = s.Trip.OriginLot.Name })
            .ToListAsync(cancellationToken);
        var transfers = await db.Transfers.AsNoTracking()
            .Where(t => t.VehicleId == vehicleId)
            .Select(t => new { t.Id, t.Status, t.CreatedAtUtc, t.DepartedAtUtc, t.ArrivedAtUtc, From = t.FromLot.Name, To = t.ToLot.Name, Driver = t.DepartedBy == null ? null : t.DepartedBy.FullName })
            .ToListAsync(cancellationToken);
        var changes = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == "Vehicle" && a.EntityId == vehicleId)
            .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new { a.Action, a.AtUtc, a.AfterJson, Admin = u.FullName })
            .ToListAsync(cancellationToken);

        var entries = segments
            .Select(s => new VehicleHistoryEntry(
                s.StartedAtUtc,
                s.EndedAtUtc,
                "Trip",
                s.Kind switch
                {
                    SegmentKind.Start => $"Trip from {s.Origin}, driven by {s.Staff}",
                    SegmentKind.VehicleSwap => $"Swapped into a trip, driven by {s.Staff}",
                    _ => $"Trip handed over to {s.Staff}",
                } + (s.EndedAtUtc is null ? " (running)" : string.Empty),
                (long)((s.EndedAtUtc ?? now) - s.StartedAtUtc).TotalSeconds,
                s.TripId,
                null))
            .Concat(transfers.Select(t => new VehicleHistoryEntry(
                t.DepartedAtUtc ?? t.CreatedAtUtc,
                t.ArrivedAtUtc,
                "Transfer",
                $"Transfer {t.From} → {t.To}: {t.Status}" + (t.Driver is null ? string.Empty : $", driven by {t.Driver}"),
                t.DepartedAtUtc is { } departed ? (long)((t.ArrivedAtUtc ?? now) - departed).TotalSeconds : null,
                null,
                t.Id)))
            .Concat(changes.Select(c => new VehicleHistoryEntry(
                c.AtUtc,
                null,
                c.Action,
                $"{c.Action} by {c.Admin}" + (c.AfterJson is null ? string.Empty : $": {c.AfterJson}"),
                null,
                null,
                null)))
            .OrderByDescending(e => e.AtUtc)
            .ToList();

        return new PagedResult<VehicleHistoryEntry>(
            entries.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToList(),
            page.Page,
            page.PageSize,
            entries.Count);
    }
}

/// <summary>
/// One history line. Kind: Trip, Transfer, or an audit action (Create, Update, StatusChange, LocationResolve).
/// </summary>
public sealed record VehicleHistoryEntry(
    DateTime AtUtc,
    DateTime? EndedAtUtc,
    string Kind,
    string Description,
    long? DurationSeconds,
    int? TripId,
    int? TransferId);
