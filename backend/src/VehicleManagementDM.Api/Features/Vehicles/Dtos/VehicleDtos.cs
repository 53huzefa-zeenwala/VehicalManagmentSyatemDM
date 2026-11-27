using System.ComponentModel.DataAnnotations;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots.Dtos;

namespace VehicleManagementDM.Api.Features.Vehicles.Dtos;

public sealed class VehicleQuery : PageQuery
{
    /// <summary>Matches registration number, brand or model.</summary>
    [StringLength(60)]
    public string? Search { get; init; }

    public VehicleStatus? Status { get; init; }

    public int? CategoryId { get; init; }

    public int? TypeId { get; init; }

    public int? LotId { get; init; }

    /// <summary>Only vehicles marked "Not at a lot" (need an admin to resolve them).</summary>
    public bool NotAtLot { get; init; }
}

public class VehicleRequest
{
    [Required(ErrorMessage = "Enter the registration number.")]
    [StringLength(30, ErrorMessage = "The registration number can have at most 30 characters.")]
    public string RegistrationNumber { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Pick a vehicle type.")]
    public int VehicleTypeId { get; init; }

    [Required(ErrorMessage = "Enter the brand.")]
    [StringLength(60, ErrorMessage = "The brand can have at most 60 characters.")]
    public string Brand { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter the model.")]
    [StringLength(60, ErrorMessage = "The model can have at most 60 characters.")]
    public string Model { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter the colour.")]
    [StringLength(30, ErrorMessage = "The colour can have at most 30 characters.")]
    public string Colour { get; init; } = string.Empty;
}

public sealed class CreateVehicleRequest : VehicleRequest
{
    /// <summary>Lot where the vehicle is parked; null = "Not at a lot".</summary>
    public int? LotId { get; init; }

    /// <summary>Available (default), Inactive or UnderMaintenance.</summary>
    public VehicleStatus Status { get; init; } = VehicleStatus.Available;
}

public sealed class UpdateVehicleRequest : VehicleRequest
{
    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

public sealed class VehicleStatusRequest
{
    [Required(ErrorMessage = "Pick a status.")]
    public VehicleStatus? Status { get; init; }

    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

public sealed class VehicleLocationRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick a parking lot.")]
    public int LotId { get; init; }

    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

public sealed record VehicleResponse(
    int Id,
    string RegistrationNumber,
    int CategoryId,
    string Category,
    int VehicleTypeId,
    string Type,
    string Brand,
    string Model,
    string Colour,
    string? PhotoUrl,
    VehicleStatus Status,
    LotRef? CurrentLot,
    bool IsNotAtLot,
    string QrToken,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    byte[] RowVersion);

/// <summary>What the app needs to draw a QR label (D-040).</summary>
public sealed record QrLabelResponse(int VehicleId, string QrToken, string RegistrationNumber, string Category, string Type);
