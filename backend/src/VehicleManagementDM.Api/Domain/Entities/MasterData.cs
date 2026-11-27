using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>Vehicle category, e.g. "Truck" (VEH-01, DATABASE_DESIGN.md §3.4).</summary>
public class VehicleCategory : IHasTimestamps, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<VehicleType> Types { get; set; } = [];
}

/// <summary>Vehicle type within a category, e.g. "Garbage truck"; lot capacities are per type (VEH-01).</summary>
public class VehicleType : IHasTimestamps, IAuditable
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public VehicleCategory Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>A fleet vehicle (VEH-02..04, QR-01, DATABASE_DESIGN.md §3.5).</summary>
public class Vehicle : IHasTimestamps, IAuditable
{
    public int Id { get; set; }

    /// <summary>As displayed, e.g. "D 01423".</summary>
    public string RegistrationNumber { get; set; } = string.Empty;

    /// <summary>Upper-case, no spaces or dashes; unique; used for typed lookup.</summary>
    public string RegistrationKey { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }

    public VehicleType VehicleType { get; set; } = null!;

    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Colour { get; set; } = string.Empty;

    public string? PhotoFileName { get; set; }

    /// <summary>Random token printed in the QR code; holds no data (QR-01) and never changes (QR-04).</summary>
    public string QrToken { get; set; } = string.Empty;

    public VehicleStatus Status { get; set; } = VehicleStatus.Available;

    /// <summary>The lot the vehicle is counted at; null = out on a trip or "Not at a lot" (DATABASE_DESIGN.md §4).</summary>
    public int? CurrentLotId { get; set; }

    public ParkingLot? CurrentLot { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

/// <summary>A parking lot (LOT-01..07, DATABASE_DESIGN.md §3.6).</summary>
public class ParkingLot : IHasTimestamps, IAuditable
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string ContactName { get; set; } = string.Empty;

    public string ContactNumber { get; set; } = string.Empty;

    /// <summary>Opening time, GST; information only (LOT-05).</summary>
    public TimeOnly? OpensAt { get; set; }

    public TimeOnly? ClosesAt { get; set; }

    public LotType LotType { get; set; }

    public int TotalCapacity { get; set; }

    /// <summary>Spaces taken by vehicles outside the system (LOT-02).</summary>
    public int BaselineOccupied { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public List<LotTypeCapacity> TypeCapacities { get; set; } = [];
}

/// <summary>Capacity and baseline of one vehicle type at one lot (LOT-01, LOT-02, P-09).</summary>
public class LotTypeCapacity : IAuditable
{
    public int LotId { get; set; }

    public int VehicleTypeId { get; set; }

    public VehicleType VehicleType { get; set; } = null!;

    public int Capacity { get; set; }

    public int BaselineOccupied { get; set; }
}

/// <summary>A destination area, e.g. "Deira" (DST-01).</summary>
public class Area : IHasTimestamps, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<Location> Locations { get; set; } = [];
}

/// <summary>A destination location within an area (DST-01).</summary>
public class Location : IHasTimestamps, IAuditable
{
    public int Id { get; set; }

    public int AreaId { get; set; }

    public Area Area { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
