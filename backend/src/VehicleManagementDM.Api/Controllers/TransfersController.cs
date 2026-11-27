using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Features.Transfers;

namespace VehicleManagementDM.Api.Controllers;

/// <summary>Transfers between lots (TRF). Admins create and cancel; staff confirm departure and arrival.</summary>
[ApiController]
[Route("api/v1/transfers")]
public sealed class TransfersController(TransferService transfers) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TransferResponse>> Create(CreateTransferRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await transfers.CreateAsync(request, User.GetUserId(), cancellationToken));

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public Task<PagedResult<TransferResponse>> List([FromQuery] TransferQuery query, CancellationToken cancellationToken) =>
        transfers.ListAsync(query, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<TransferResponse> Get(int id, CancellationToken cancellationToken) =>
        transfers.GetAsync(id, null, cancellationToken);

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = Roles.Admin)]
    public Task<TransferResponse> Cancel(int id, CancellationToken cancellationToken) =>
        transfers.CancelAsync(id, User.GetUserId(), cancellationToken);

    /// <summary>Transfers waiting for a driver, and transfers on the road (S-22).</summary>
    [HttpGet("tasks")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TransferTasksResponse> Tasks(CancellationToken cancellationToken) =>
        transfers.TasksAsync(cancellationToken);

    /// <summary>The open transfer of a scanned vehicle.</summary>
    [HttpGet("by-vehicle")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TransferResponse> ByVehicle([FromQuery, Required(ErrorMessage = "Scan the vehicle's QR code."), StringLength(22)] string qr, CancellationToken cancellationToken) =>
        transfers.ByVehicleQrAsync(qr, cancellationToken);

    /// <summary>I take the vehicle: departure confirmed, and I am the driver (TRF-02).</summary>
    [HttpPost("{id:int}/depart")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TransferResponse> Depart(int id, CancellationToken cancellationToken) =>
        transfers.DepartAsync(id, User.GetUserId(), cancellationToken);

    [HttpPost("{id:int}/arrive")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TransferResponse> Arrive(int id, CancellationToken cancellationToken) =>
        transfers.ArriveAsync(id, User.GetUserId(), cancellationToken);
}
