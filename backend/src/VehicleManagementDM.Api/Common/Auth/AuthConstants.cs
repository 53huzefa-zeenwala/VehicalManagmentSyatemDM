using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace VehicleManagementDM.Api.Common.Auth;

public static class AuthConstants
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

    /// <summary>Name of the session cookie (D-031).</summary>
    public const string CookieName = "vms.auth";

    /// <summary>Claim holding the user's security stamp; checked on every request (AUTH-07).</summary>
    public const string SecurityStampClaim = "stamp";

    /// <summary>Rate-limiter policy applied to the login endpoint.</summary>
    public const string LoginRateLimitPolicy = "login";
}

/// <summary>Role names for <c>[Authorize(Roles = ...)]</c>; match <see cref="Domain.Enums.UserRole"/>.</summary>
public static class Roles
{
    public const string Admin = nameof(Domain.Enums.UserRole.Admin);
    public const string Staff = nameof(Domain.Enums.UserRole.Staff);
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's ID. Only call on endpoints that require authentication.</summary>
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("The request has no signed-in user.");
}
