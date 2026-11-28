using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
        var lotNames = await db.ParkingLots.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);

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
                DescribeChange(c.Action, c.Admin, c.AfterJson, lotNames),
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

    /// <summary>
    /// Readable line for an admin change, from the audit entry's changed values (never the raw JSON: it would show
    /// internal fields such as the QR token).
    /// </summary>
    internal static string DescribeChange(string action, string admin, string? afterJson, IReadOnlyDictionary<int, string> lotNames)
    {
        var after = afterJson is null ? null : JsonNode.Parse(afterJson) as JsonObject;
        var status = after?["status"]?.GetValue<string>();
        var lotId = after?["currentLotId"]?.GetValue<int?>();
        var lotName = lotId.HasValue && lotNames.TryGetValue(lotId.Value, out var name) ? name : null;
        switch (action)
        {
            case "Create":
                return $"Added by {admin}";
            case "StatusChange" when status is not null:
                return $"Status set to {Humanize(status).ToLowerInvariant()} by {admin}";
            case "LocationResolve":
                return lotName is null ? $"Location changed by {admin}" : $"Placed at {lotName} by {admin}";
            case "Update" when after is { Count: 1 } && after.ContainsKey("photoFileName"):
                return $"Photo changed by {admin}";
            case "Update" when after is { Count: > 0 }:
                var fields = after.Select(p => Humanize(p.Key).ToLowerInvariant()).Where(f => f != "qr token" && f != "photo file name");
                var list = string.Join(", ", fields);
                return list.Length == 0 ? $"Edited by {admin}" : $"Edited by {admin}: {list}";
            default:
                return $"{Humanize(action)} by {admin}";
        }
    }

    /// <summary>"UnderMaintenance" / "registrationNumber" → "Under maintenance" / "Registration number".</summary>
    private static string Humanize(string name)
    {
        var spaced = Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        return spaced.Length == 0 ? spaced : char.ToUpperInvariant(spaced[0]) + spaced[1..].ToLowerInvariant();
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
