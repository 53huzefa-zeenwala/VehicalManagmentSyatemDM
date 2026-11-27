using System.ComponentModel.DataAnnotations;

namespace VehicleManagementDM.Api.Features.VehicleCategories;

public sealed class NameRequest
{
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(60, ErrorMessage = "The name can have at most 60 characters.")]
    public string Name { get; init; } = string.Empty;
}

public sealed class NameAndStatusRequest
{
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(60, ErrorMessage = "The name can have at most 60 characters.")]
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed record CategoryResponse(int Id, string Name, bool IsActive, IReadOnlyList<TypeResponse> Types);

public sealed record TypeResponse(int Id, string Name, bool IsActive);
