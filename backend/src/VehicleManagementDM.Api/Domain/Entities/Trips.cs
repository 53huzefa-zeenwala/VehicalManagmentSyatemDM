using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>
/// One period a vehicle spends outside a lot, from Start to End or Cancel (TRP, DATABASE_DESIGN.md §3.8).
/// Swaps and hand-overs keep the same trip and add a segment.
/// </summary>
public class Trip
{
    public int Id { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Active;

    public int OriginLotId { get; set; }

    public ParkingLot OriginLot { get; set; } = null!;

    public int DestinationLocationId { get; set; }

    public Location DestinationLocation { get; set; } = null!;

    /// <summary>The staff member (driver) now responsible for the trip; changes on hand-over.</summary>
    public int CurrentStaffId { get; set; }

    public User CurrentStaff { get; set; } = null!;

    /// <summary>The vehicle now on the trip; changes on a vehicle swap.</summary>
    public int CurrentVehicleId { get; set; }

    public Vehicle CurrentVehicle { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    /// <summary>End: the return lot. Cancel: the lot the vehicle was left at; null = "Not at a lot".</summary>
    public int? ReturnLotId { get; set; }

    public ParkingLot? ReturnLot { get; set; }

    public CancelCategory? CancelCategory { get; set; }

    public string? CancelNote { get; set; }

    /// <summary>When the next reminder is due (TRP-06); null once the trip is closed.</summary>
    public DateTime? NextReminderAtUtc { get; set; }

    public int ReminderCount { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public List<TripSegment> Segments { get; set; } = [];
}

/// <summary>Part of a trip with one staff member and one vehicle; hours are counted per segment (BR-06).</summary>
public class TripSegment
{
    public int Id { get; set; }

    public int TripId { get; set; }

    public Trip Trip { get; set; } = null!;

    /// <summary>1, 2, 3… within the trip.</summary>
    public int Seq { get; set; }

    public SegmentKind Kind { get; set; }

    public int StaffId { get; set; }

    public User Staff { get; set; } = null!;

    public int VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    /// <summary>Why the vehicle was swapped or the trip handed over (P-03).</summary>
    public string? Reason { get; set; }

    /// <summary>The staff member's signature, on the Start segment only (TRP-01, D-041).</summary>
    public string? SignatureFileName { get; set; }

    /// <summary>Vehicle swap: the lot where the previous vehicle was left; null = "Not at a lot".</summary>
    public int? OutgoingVehicleLotId { get; set; }

    public ParkingLot? OutgoingVehicleLot { get; set; }
}
