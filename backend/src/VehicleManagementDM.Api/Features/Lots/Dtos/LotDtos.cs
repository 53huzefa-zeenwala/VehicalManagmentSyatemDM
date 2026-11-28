using System.ComponentModel.DataAnnotations;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Features.Lots.Dtos;

/// <summary>Short reference to a lot, used inside other responses.</summary>
public sealed record LotRef(int Id, string Code, string Name);

public class LotRequest
{
    [Required(ErrorMessage = "Enter a lot code.")]
    [StringLength(20, ErrorMessage = "The code can have at most 20 characters.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "The code can contain only letters, digits and dashes.")]
    public string Code { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter the address.")]
    [StringLength(300, ErrorMessage = "The address can have at most 300 characters.")]
    public string Address { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter a contact person.")]
    [StringLength(100, ErrorMessage = "The contact name can have at most 100 characters.")]
    public string ContactName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter a contact number.")]
    [PhoneNumber]
    public string ContactNumber { get; init; } = string.Empty;

    [TimeOfDay]
    public string? OpensAt { get; init; }

    [TimeOfDay]
    public string? ClosesAt { get; init; }

    [Required(ErrorMessage = "Pick the lot type.")]
    public LotType? LotType { get; init; }

    [Required(ErrorMessage = "Enter the total capacity.")]
    [Range(0, 100_000, ErrorMessage = "Total capacity must be between 0 and 100,000.")]
    public int? TotalCapacity { get; init; }

    [Range(0, 100_000, ErrorMessage = "The baseline count must be between 0 and 100,000.")]
    public int BaselineOccupied { get; init; }

    public List<TypeCapacityRequest> TypeCapacities { get; init; } = [];
}

public sealed class UpdateLotRequest : LotRequest
{
    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

public sealed class TypeCapacityRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick a vehicle type.")]
    public int VehicleTypeId { get; init; }

    [Range(0, 100_000, ErrorMessage = "Capacity must be between 0 and 100,000.")]
    public int Capacity { get; init; }

    [Range(0, 100_000, ErrorMessage = "The baseline count must be between 0 and 100,000.")]
    public int BaselineOccupied { get; init; }
}

public sealed class LotStatusRequest
{
    public bool IsActive { get; init; }

    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

/// <summary>Lot with live occupancy (BR-05), used for the list and the detail screen.</summary>
public sealed record LotResponse(
    int Id,
    string Code,
    string Name,
    string Address,
    string ContactName,
    string ContactNumber,
    string? OpensAt,
    string? ClosesAt,
    LotType LotType,
    bool IsActive,
    int BaselineOccupied,
    OccupancyTotal Total,
    IReadOnlyList<OccupancyByType> ByType,
    byte[] RowVersion);

/// <summary>Whole-lot figures. Available can be negative: full lots warn, never block (BR-09).</summary>
public sealed record OccupancyTotal(int Capacity, int Occupied, int Available, bool IsFull, bool IsOverCapacity);

/// <summary>Per vehicle type. Capacity and Available are null when the lot sets no capacity for that type.</summary>
public sealed record OccupancyByType(
    int VehicleTypeId,
    string Type,
    string Category,
    int? Capacity,
    int BaselineOccupied,
    int Occupied,
    int? Available);
