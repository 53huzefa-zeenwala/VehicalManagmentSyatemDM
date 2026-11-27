using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Features.VehicleCategories;

/// <summary>Vehicle categories and their types (VEH-01). Admins write; everyone signed in reads.</summary>
[ApiController]
[Route("api/v1")]
public sealed class VehicleCategoriesController(AppDbContext db) : ControllerBase
{
    /// <summary>Categories with their types. Staff always get active ones only.</summary>
    [HttpGet("vehicle-categories")]
    public async Task<IReadOnlyList<CategoryResponse>> List([FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var showInactive = includeInactive && User.IsInRole(Roles.Admin);
        var categories = await db.VehicleCategories
            .AsNoTracking()
            .Where(c => showInactive || c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.IsActive,
                c.Types.Where(t => showInactive || t.IsActive).OrderBy(t => t.Name).Select(t => new TypeResponse(t.Id, t.Name, t.IsActive)).ToList()))
            .ToListAsync(cancellationToken);
        return categories;
    }

    [HttpPost("vehicle-categories")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(NameRequest request, CancellationToken cancellationToken)
    {
        var category = new VehicleCategory { Name = Text.Clean(request.Name) };
        db.VehicleCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new CategoryResponse(category.Id, category.Name, category.IsActive, []));
    }

    [HttpPut("vehicle-categories/{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<CategoryResponse> UpdateCategory(int id, NameAndStatusRequest request, CancellationToken cancellationToken)
    {
        var category = await db.VehicleCategories.Include(c => c.Types).SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        category.Name = Text.Clean(request.Name);
        category.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.IsActive,
            category.Types.OrderBy(t => t.Name).Select(t => new TypeResponse(t.Id, t.Name, t.IsActive)).ToList());
    }

    [HttpPost("vehicle-categories/{id:int}/types")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TypeResponse>> CreateType(int id, NameRequest request, CancellationToken cancellationToken)
    {
        var category = await db.VehicleCategories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        if (!category.IsActive)
        {
            throw BadRequestException.ForField("categoryId", "This category is inactive. Reactivate it before adding types.");
        }

        var type = new VehicleType { CategoryId = id, Name = Text.Clean(request.Name) };
        db.VehicleTypes.Add(type);
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new TypeResponse(type.Id, type.Name, type.IsActive));
    }

    [HttpPut("vehicle-types/{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<TypeResponse> UpdateType(int id, NameAndStatusRequest request, CancellationToken cancellationToken)
    {
        var type = await db.VehicleTypes.SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Vehicle type not found.");
        type.Name = Text.Clean(request.Name);
        type.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return new TypeResponse(type.Id, type.Name, type.IsActive);
    }
}

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
