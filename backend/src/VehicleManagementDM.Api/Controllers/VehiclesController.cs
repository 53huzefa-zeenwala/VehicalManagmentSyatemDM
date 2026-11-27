using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Features.Vehicles;
using VehicleManagementDM.Api.Features.Vehicles.Dtos;

namespace VehicleManagementDM.Api.Controllers;

/// <summary>Vehicle catalogue management for admins (VEH, QR). Staff lookup by QR arrives with trips (M3).</summary>
[ApiController]
[Route("api/v1/vehicles")]
[Authorize(Roles = Roles.Admin)]
public sealed class VehiclesController(VehicleService vehicles, VehicleHistoryService history) : ControllerBase
{
    /// <summary>Photo uploads: 20 MB photo plus multipart overhead (ARCHITECTURE.md §11).</summary>
    public const long PhotoRequestLimit = 21 * 1024 * 1024;

    [HttpGet]
    public Task<PagedResult<VehicleResponse>> List([FromQuery] VehicleQuery query, CancellationToken cancellationToken) =>
        vehicles.ListAsync(query, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<VehicleResponse> Get(int id, CancellationToken cancellationToken) =>
        vehicles.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create(CreateVehicleRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await vehicles.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public Task<VehicleResponse> Update(int id, UpdateVehicleRequest request, CancellationToken cancellationToken) =>
        vehicles.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/status")]
    public Task<VehicleResponse> SetStatus(int id, VehicleStatusRequest request, CancellationToken cancellationToken) =>
        vehicles.SetStatusAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/location")]
    public Task<VehicleResponse> SetLocation(int id, VehicleLocationRequest request, CancellationToken cancellationToken) =>
        vehicles.SetLocationAsync(id, request, cancellationToken);

    /// <summary>Uploads the vehicle photo as multipart form field "file" (JPEG or PNG, up to 20 MB).</summary>
    [HttpPut("{id:int}/photo")]
    [RequestSizeLimit(PhotoRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = PhotoRequestLimit)]
    public async Task<VehicleResponse> SetPhoto(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw BadRequestException.ForField("file", "Choose a photo to upload.");
        }

        await using var content = file.OpenReadStream();
        return await vehicles.SetPhotoAsync(id, content, cancellationToken);
    }

    /// <summary>Trips, transfers and admin changes of one vehicle, newest first (VEH-05).</summary>
    [HttpGet("{id:int}/history")]
    public Task<PagedResult<VehicleHistoryEntry>> History(int id, [FromQuery] PageQuery page, CancellationToken cancellationToken) =>
        history.HistoryAsync(id, page, cancellationToken);

    /// <summary>QR label data for one or more vehicles: <c>?ids=1&amp;ids=2</c> (D-040).</summary>
    [HttpGet("qr-labels")]
    public Task<IReadOnlyList<QrLabelResponse>> QrLabels([FromQuery] int[] ids, CancellationToken cancellationToken) =>
        vehicles.QrLabelsAsync(ids, cancellationToken);
}
