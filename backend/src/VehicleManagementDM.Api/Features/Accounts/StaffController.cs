using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Vehicles;

namespace VehicleManagementDM.Api.Features.Accounts;

/// <summary>Staff management for admins (STF-01..02). Trips and hours per staff member arrive with trips (M3).</summary>
[ApiController]
[Route("api/v1/staff")]
[Authorize(Roles = Roles.Admin)]
public sealed class StaffController(AccountService accounts) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<StaffResponse>> List([FromQuery] StaffQuery query, CancellationToken cancellationToken) =>
        accounts.ListStaffAsync(query, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<StaffResponse> Get(int id, CancellationToken cancellationToken) =>
        accounts.GetStaffAsync(id, cancellationToken);

    /// <summary>Creates a staff account; the response carries the generated Staff ID (AUTH-03).</summary>
    [HttpPost]
    public async Task<ActionResult<StaffResponse>> Create(CreateStaffRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await accounts.CreateStaffAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public Task<StaffResponse> Update(int id, UpdateStaffRequest request, CancellationToken cancellationToken) =>
        accounts.UpdateStaffAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/status")]
    public async Task<StaffResponse> SetStatus(int id, AccountStatusRequest request, CancellationToken cancellationToken)
    {
        await accounts.SetStatusAsync(id, UserRole.Staff, request, User.GetUserId(), cancellationToken);
        return await accounts.GetStaffAsync(id, cancellationToken);
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await accounts.ResetPasswordAsync(id, UserRole.Staff, request.NewPassword, cancellationToken);
        return NoContent();
    }

    /// <summary>Uploads the staff photo as multipart form field "file" (JPEG or PNG, up to 20 MB).</summary>
    [HttpPut("{id:int}/photo")]
    [RequestSizeLimit(VehiclesController.PhotoRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = VehiclesController.PhotoRequestLimit)]
    public async Task<StaffResponse> SetPhoto(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw BadRequestException.ForField("file", "Choose a photo to upload.");
        }

        await using var content = file.OpenReadStream();
        return await accounts.SetStaffPhotoAsync(id, content, cancellationToken);
    }
}

/// <summary>Admin accounts (AUTH-05, AUTH-06). Every admin has full access.</summary>
[ApiController]
[Route("api/v1/admins")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminsController(AccountService accounts) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminResponse>> List(CancellationToken cancellationToken) =>
        accounts.ListAdminsAsync(cancellationToken);

    [HttpGet("{id:int}")]
    public Task<AdminResponse> Get(int id, CancellationToken cancellationToken) =>
        accounts.GetAdminAsync(id, cancellationToken);

    /// <summary>Creates an admin account; the response carries the generated Admin ID (D-037).</summary>
    [HttpPost]
    public async Task<ActionResult<AdminResponse>> Create(CreateAdminRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await accounts.CreateAdminAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public Task<AdminResponse> Update(int id, UpdateAdminRequest request, CancellationToken cancellationToken) =>
        accounts.UpdateAdminAsync(id, request, cancellationToken);

    /// <summary>Deactivates or reactivates an admin; admins can't deactivate themselves.</summary>
    [HttpPut("{id:int}/status")]
    public async Task<AdminResponse> SetStatus(int id, AccountStatusRequest request, CancellationToken cancellationToken)
    {
        await accounts.SetStatusAsync(id, UserRole.Admin, request, User.GetUserId(), cancellationToken);
        return await accounts.GetAdminAsync(id, cancellationToken);
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await accounts.ResetPasswordAsync(id, UserRole.Admin, request.NewPassword, cancellationToken);
        return NoContent();
    }
}
