using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Features.Lots.Dtos;

namespace VehicleManagementDM.Api.Features.Lots;

/// <summary>Parking lots (LOT-01..07). Everyone signed in sees lots and availability; admins manage them.</summary>
[ApiController]
[Route("api/v1/lots")]
public sealed class LotsController(LotService lots) : ControllerBase
{
    /// <summary>All lots with occupancy, for the list and the map (LOT-06). Staff see active lots only.</summary>
    [HttpGet]
    public Task<IReadOnlyList<LotResponse>> List([FromQuery] bool includeInactive, CancellationToken cancellationToken) =>
        lots.ListAsync(includeInactive && User.IsInRole(Roles.Admin), cancellationToken);

    [HttpGet("{id:int}")]
    public Task<LotResponse> Get(int id, CancellationToken cancellationToken) =>
        lots.GetAsync(id, includeInactive: User.IsInRole(Roles.Admin), cancellationToken);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<LotResponse>> Create(LotRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await lots.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<LotResponse> Update(int id, UpdateLotRequest request, CancellationToken cancellationToken) =>
        lots.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.Admin)]
    public Task<LotResponse> SetStatus(int id, LotStatusRequest request, CancellationToken cancellationToken) =>
        lots.SetStatusAsync(id, request, cancellationToken);
}
