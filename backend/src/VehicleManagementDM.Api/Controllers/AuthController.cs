using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Features.Auth;
using VehicleManagementDM.Api.Features.Auth.Dtos;

namespace VehicleManagementDM.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Signs in with ID and password and sets the session cookie (AUTH-01).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthConstants.LoginRateLimitPolicy)]
    public Task<MeResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    /// <summary>Ends the session and clears the cookie.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync();
        return NoContent();
    }

    /// <summary>The signed-in user.</summary>
    [HttpGet("me")]
    public Task<MeResponse> Me(CancellationToken cancellationToken) =>
        authService.GetMeAsync(User.GetUserId(), cancellationToken);

    /// <summary>Changes the caller's own password; other sessions end (P-14, AUTH-07).</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }
}
