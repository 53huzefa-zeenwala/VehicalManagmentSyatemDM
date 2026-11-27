using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>
/// A move of a vehicle between lots, created by an admin (TRF, DATABASE_DESIGN.md §3.10). Whoever confirms
/// departure drives it (D-020); any staff member may confirm departure and arrival (D-047).
/// </summary>
public class Transfer : IAuditable
{
    public int Id { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.PendingDeparture;

    public int VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    public int FromLotId { get; set; }

    public ParkingLot FromLot { get; set; } = null!;

    public int ToLotId { get; set; }

    public ParkingLot ToLot { get; set; } = null!;

    public string? Note { get; set; }

    public int CreatedByUserId { get; set; }

    public User CreatedBy { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>The staff member who confirmed departure; they drive the vehicle (TRF-02).</summary>
    public int? DepartedByUserId { get; set; }

    public User? DepartedBy { get; set; }

    public DateTime? DepartedAtUtc { get; set; }

    public int? ArrivedByUserId { get; set; }

    public User? ArrivedBy { get; set; }

    public DateTime? ArrivedAtUtc { get; set; }

    public int? CancelledByUserId { get; set; }

    public User? CancelledBy { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    /// <summary>Reminders while in transit follow the trip rule (TRF-07).</summary>
    public DateTime? NextReminderAtUtc { get; set; }

    public int ReminderCount { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

/// <summary>One reminder sent to one person about a trip or a transfer (TRP-06, TRF-07, A-04).</summary>
public class ReminderLog
{
    public long Id { get; set; }

    public int? TripId { get; set; }

    public int? TransferId { get; set; }

    public int RecipientUserId { get; set; }

    public int ReminderNumber { get; set; }

    public DateTime SentAtUtc { get; set; }
}

/// <summary>An entry in a user's in-app notification list (NTF-03, D-039).</summary>
public class Notification
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>What tapping the notification opens: "Trip" or "Transfer".</summary>
    public string? TargetType { get; set; }

    public int? TargetId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}
