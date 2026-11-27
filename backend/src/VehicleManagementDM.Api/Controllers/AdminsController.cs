using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Accounts;

namespace VehicleManagementDM.Api.Controllers;

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
