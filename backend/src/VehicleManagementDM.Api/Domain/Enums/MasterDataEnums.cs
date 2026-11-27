namespace VehicleManagementDM.Api.Domain.Enums;

/// <summary>Vehicle status (VEH-03). Active is set only by trips and transfers.</summary>
public enum VehicleStatus
{
    Available,
    Active,
    Inactive,
    UnderMaintenance,
}

/// <summary>Trip state (BR-10). There is no "ended by admin" (D-021).</summary>
public enum TripStatus
{
    Active,
    Ended,
    Cancelled,
}

/// <summary>How a trip segment began (D-012, D-020).</summary>
public enum SegmentKind
{
    Start,
    VehicleSwap,
    HandOver,
}

/// <summary>Reason category when a trip is cancelled (TRP-08).</summary>
public enum CancelCategory
{
    Driver,
    Vehicle,
    Technical,
    Other,
}

/// <summary>Transfer state (BR-10): PendingDeparture → InTransit → Completed, or PendingDeparture → Cancelled.</summary>
public enum TransferStatus
{
    PendingDeparture,
    InTransit,
    Completed,
    Cancelled,
}

/// <summary>Kinds of in-app notification (NTF-01..03).</summary>
public enum NotificationType
{
    TripReminder,
    TripOverdue,
    TransferReminder,
    TransferOverdue,
    TransferAwaitingDeparture,
    TransferAwaitingArrival,
    TripCancelled,
    VehicleReleased,
}

/// <summary>Parking lot type (LOT-01).</summary>
public enum LotType
{
    Open,
    Underground,
    Other,
}
