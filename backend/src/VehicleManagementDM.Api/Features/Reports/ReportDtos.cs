using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Transfers;
using VehicleManagementDM.Api.Features.Trips.Dtos;

namespace VehicleManagementDM.Api.Features.Reports;

/// <summary>A GST date range, both days inclusive, at most 366 days (RPT-02).</summary>
public class ReportRange : IValidatableObject
{
    public const int MaxDays = 366;

    [Required(ErrorMessage = "Pick a start date.")]
    public DateOnly? From { get; init; }

    [Required(ErrorMessage = "Pick an end date.")]
    public DateOnly? To { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From is { } from && To is { } to)
        {
            if (to < from)
            {
                yield return new ValidationResult("The end date is before the start date.", ["to"]);
            }
            else if (to.DayNumber - from.DayNumber + 1 > MaxDays)
            {
                yield return new ValidationResult($"Pick at most {MaxDays} days.", ["to"]);
            }
        }
    }
}

public sealed class VehicleHoursQuery : ReportRange
{
    /// <summary>Trips that started from, and transfers that left, this lot.</summary>
    public int? LotId { get; init; }

    public int? CategoryId { get; init; }

    public int? TypeId { get; init; }

    public int? VehicleId { get; init; }
}

public sealed class StaffHoursQuery : ReportRange
{
    public int? StaffId { get; init; }
}

public sealed class LotUtilizationQuery
{
    /// <summary>Optional GST date range for movement counts; defaults to today.</summary>
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public int? LotId { get; init; }
}

public sealed class AuditLogQuery : PageQuery
{
    public int? UserId { get; init; }

    [StringLength(40)]
    public string? EntityType { get; init; }

    public int? EntityId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

// ----- Responses -----

public sealed record DaySeconds(DateOnly Date, long Seconds);

public sealed record VehicleHoursRow(VehicleRef Vehicle, long TripSeconds, long TransferSeconds, long TotalSeconds, IReadOnlyList<DaySeconds> Days);

public sealed record VehicleHoursReport(long TotalSeconds, int Vehicles, IReadOnlyList<VehicleHoursRow> Rows);

public sealed record StaffHoursRow(StaffRef Staff, int Trips, int Transfers, long TripSeconds, long TransferSeconds, long TotalSeconds, IReadOnlyList<DaySeconds> Days);

public sealed record StaffHoursReport(long TotalSeconds, int Staff, IReadOnlyList<StaffHoursRow> Rows);

public sealed record LotUtilizationRow(
    LotRef Lot,
    bool IsActive,
    OccupancyTotal Total,
    IReadOnlyList<OccupancyByType> ByType,
    int TripsStarted,
    int TripsReturned,
    int TransfersOut,
    int TransfersIn);

public sealed record LotUtilizationReport(DateOnly From, DateOnly To, IReadOnlyList<LotUtilizationRow> Rows);

public sealed record HourCount(int Hour, int Trips);

public sealed record LotAttention(LotRef Lot, int Capacity, int Occupied);

public sealed record NeedsAttention(
    int OverdueTrips,
    IReadOnlyList<TripSummaryResponse> OverdueTripList,
    int OverdueTransfers,
    int VehiclesNotAtLot,
    IReadOnlyList<VehicleRef> VehiclesNotAtLotList,
    IReadOnlyList<LotAttention> LotsOverCapacity,
    int TransfersWaiting,
    IReadOnlyList<TransferResponse> TransfersWaitingList);

/// <summary>Admin home (A-01, RPT-01). Figures are for the current GST day unless named otherwise.</summary>
public sealed record DashboardResponse(
    int OnTripNow,
    int TransfersInTransit,
    int TripsToday,
    long SecondsOutToday,
    int FreeSpaces,
    IReadOnlyList<HourCount> TripsPerHourToday,
    NeedsAttention NeedsAttention);

public sealed record AuditUser(int Id, string LoginId, string FullName);

public sealed record AuditLogResponse(
    long Id,
    DateTime AtUtc,
    AuditUser User,
    string Action,
    string EntityType,
    int EntityId,
    JsonNode? Before,
    JsonNode? After);
