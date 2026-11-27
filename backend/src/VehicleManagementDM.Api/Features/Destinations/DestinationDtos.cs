using System.ComponentModel.DataAnnotations;

namespace VehicleManagementDM.Api.Features.Destinations;

public sealed class AreaRequest
{
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters.")]
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed class LocationRequest
{
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(150, ErrorMessage = "The name can have at most 150 characters.")]
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed record AreaResponse(int Id, string Name, bool IsActive, IReadOnlyList<LocationResponse> Locations);

public sealed record LocationResponse(int Id, string Name, bool IsActive);
