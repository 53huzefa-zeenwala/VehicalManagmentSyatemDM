using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Data;

namespace VehicleManagementDM.Api.Common.Auth;

/// <summary>
/// Cookie behaviour for an API (ARCHITECTURE.md §4):
/// every request re-checks that the account is active and its security stamp unchanged (AUTH-07),
/// and auth failures answer with the JSON error format instead of redirecting to a login page.
/// </summary>
public sealed class SessionCookieEvents(AppDbContext db, ApiErrorWriter writer) : CookieAuthenticationEvents
{
    private const string SessionEndReasonKey = "vms.session-end-reason";

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var stamp = principal?.FindFirstValue(AuthConstants.SecurityStampClaim);
        if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || stamp is null)
        {
            await EndSessionAsync(context, ErrorCodes.Unauthenticated);
            return;
        }

        var account = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.SecurityStamp })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        // Deactivation is checked first so the app can say why the session ended (deactivation also rotates the stamp).
        if (account is { IsActive: false })
        {
            await EndSessionAsync(context, ErrorCodes.AccountDeactivated);
        }
        else if (account is null || account.SecurityStamp != stamp)
        {
            await EndSessionAsync(context, ErrorCodes.Unauthenticated);
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        var deactivated = context.HttpContext.Items[SessionEndReasonKey] as string == ErrorCodes.AccountDeactivated;
        var error = deactivated
            ? new ApiError
            {
                Status = StatusCodes.Status401Unauthorized,
                Code = ErrorCodes.AccountDeactivated,
                Title = "Account deactivated",
                Detail = "Your account has been deactivated. Contact your administrator.",
            }
            : new ApiError
            {
                Status = StatusCodes.Status401Unauthorized,
                Code = ErrorCodes.Unauthenticated,
                Title = "Sign in required",
                Detail = "Your session has ended. Please sign in again.",
            };
        return writer.WriteAsync(context.HttpContext, error);
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) =>
        writer.WriteAsync(context.HttpContext, new ApiError
        {
            Status = StatusCodes.Status403Forbidden,
            Code = ErrorCodes.Forbidden,
            Title = "Not allowed",
            Detail = "Your account doesn't have permission to do this.",
        });

    public override Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context) => Task.CompletedTask;

    public override Task RedirectToReturnUrl(RedirectContext<CookieAuthenticationOptions> context) => Task.CompletedTask;

    private static async Task EndSessionAsync(CookieValidatePrincipalContext context, string reason)
    {
        context.HttpContext.Items[SessionEndReasonKey] = reason;
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AuthConstants.Scheme);
    }
}
