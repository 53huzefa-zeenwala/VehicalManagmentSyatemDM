using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Features.Destinations;

namespace VehicleManagementDM.Api.Controllers;

/// <summary>Destinations as Area → Location (DST-01). Admins write; everyone signed in reads.</summary>
[ApiController]
[Route("api/v1")]
public sealed class DestinationsController(AppDbContext db) : ControllerBase
{
    /// <summary>Areas with their locations. Staff always get active ones only.</summary>
    [HttpGet("areas")]
    public async Task<IReadOnlyList<AreaResponse>> List([FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var showInactive = includeInactive && User.IsInRole(Roles.Admin);
        return await db.Areas
            .AsNoTracking()
            .Where(a => showInactive || a.IsActive)
            .OrderBy(a => a.Name)
            .Select(a => new AreaResponse(
                a.Id,
                a.Name,
                a.IsActive,
                a.Locations.Where(l => showInactive || l.IsActive).OrderBy(l => l.Name).Select(l => new LocationResponse(l.Id, l.Name, l.IsActive)).ToList()))
            .ToListAsync(cancellationToken);
    }

    [HttpPost("areas")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AreaResponse>> CreateArea(AreaRequest request, CancellationToken cancellationToken)
    {
        var area = new Area { Name = Text.Clean(request.Name) };
        db.Areas.Add(area);
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new AreaResponse(area.Id, area.Name, area.IsActive, []));
    }

    [HttpPut("areas/{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<AreaResponse> UpdateArea(int id, AreaRequest request, CancellationToken cancellationToken)
    {
        var area = await db.Areas.Include(a => a.Locations).SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Area not found.");
        area.Name = Text.Clean(request.Name);
        area.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return new AreaResponse(
            area.Id,
            area.Name,
            area.IsActive,
            area.Locations.OrderBy(l => l.Name).Select(l => new LocationResponse(l.Id, l.Name, l.IsActive)).ToList());
    }

    [HttpPost("areas/{id:int}/locations")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<LocationResponse>> CreateLocation(int id, LocationRequest request, CancellationToken cancellationToken)
    {
        var area = await db.Areas.SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Area not found.");
        if (!area.IsActive)
        {
            throw BadRequestException.ForField("areaId", "This area is inactive. Reactivate it before adding locations.");
        }

        var location = new Location { AreaId = id, Name = Text.Clean(request.Name) };
        db.Locations.Add(location);
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new LocationResponse(location.Id, location.Name, location.IsActive));
    }

    [HttpPut("locations/{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<LocationResponse> UpdateLocation(int id, LocationRequest request, CancellationToken cancellationToken)
    {
        var location = await db.Locations.SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Location not found.");
        location.Name = Text.Clean(request.Name);
        location.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return new LocationResponse(location.Id, location.Name, location.IsActive);
    }
}
