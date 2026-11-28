using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Common.Time;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Auth.Dtos;
using VehicleManagementDM.Api.Features.Trips;

namespace VehicleManagementDM.Api.Features.Auth;

/// <summary>Sign-in, sign-out and password change (AUTH-01, 02, 07, 08; D-031).</summary>
public sealed class AuthService(
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuthOptions> authOptions,
    TripQueries tripQueries,
    AppVersionGate versionGate,
    TimeProvider timeProvider)
{
    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No current HTTP request.");

    public async Task<MeResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var options = authOptions.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var loginId = request.LoginId.Trim().ToUpperInvariant();

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.LoginId == loginId, cancellationToken);
        if (user is null)
        {
            throw InvalidCredentials();
        }

        if (user.LockoutEndUtc > now)
        {
            throw Locked(user.LockoutEndUtc.Value);
        }

        if (!Passwords.Matches(user.Password, request.Password))
        {
            await RecordFailedLoginAsync(user, options, now, cancellationToken);
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException(
                ErrorCodes.AccountDeactivated,
                "Account deactivated",
                "Your account has been deactivated. Contact your administrator.");
        }

        // An outdated app can't sign in either (D-058); checked only after the password, so it reveals nothing.
        var appVersion = httpContextAccessor.HttpContext?.Request.Headers[AppVersionGate.Header].ToString();
        await versionGate.EnsureSupportedAsync(user.Role, appVersion, cancellationToken);
        await versionGate.RecordAsync(user.Id, appVersion, cancellationToken);

        // Counter updates use direct UPDATEs so parallel sign-ins never fail with a concurrency conflict.
        if (user.FailedLoginCount != 0 || user.LockoutEndUtc is not null)
        {
            await db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(
                s => s.SetProperty(u => u.FailedLoginCount, 0).SetProperty(u => u.LockoutEndUtc, (DateTime?)null),
                cancellationToken);
        }

        await SignInAsync(user, request.RememberMe, options);
        return await GetMeAsync(user.Id, cancellationToken);
    }

    public Task LogoutAsync() => HttpContext.SignOutAsync(AuthConstants.Scheme);

    public async Task<MeResponse> GetMeAsync(int userId, CancellationToken cancellationToken)
    {
        var me = await LoadMeAsync(userId, cancellationToken);
        if (me.Role != UserRole.Staff)
        {
            return me;
        }

        var hasActiveTrip = await db.Trips.AnyAsync(t => t.Status == TripStatus.Active && t.CurrentStaffId == userId, cancellationToken);
        return me with { HasActiveTrip = hasActiveTrip, ThisMonth = await tripQueries.MonthStatsAsync(userId, cancellationToken) };
    }

    private async Task<MeResponse> LoadMeAsync(int userId, CancellationToken cancellationToken) =>
        await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new MeResponse(
                u.Id,
                u.LoginId,
                u.Role,
                u.FullName,
                u.Mobile,
                u.Email,
                u.LicenceNumber,
                u.PhotoFileName == null ? null : FileStore.Url(FileKind.StaffPhoto, u.PhotoFileName),
                false,
                null))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Your account was not found.");

    /// <summary>Changes the caller's password. Other sessions end; this one continues (AUTH-07).</summary>
    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Your account was not found.");

        if (!Passwords.Matches(user.Password, request.CurrentPassword))
        {
            throw BadRequestException.ForField("currentPassword", "Your current password is incorrect.");
        }

        if (request.NewPassword == request.CurrentPassword)
        {
            throw BadRequestException.ForField("newPassword", "The new password must be different from the current one.");
        }

        user.Password = request.NewPassword;
        user.SecurityStamp = User.NewSecurityStamp();
        await db.SaveChangesAsync(cancellationToken);

        var current = await HttpContext.AuthenticateAsync(AuthConstants.Scheme);
        await SignInAsync(user, current.Properties?.IsPersistent ?? false, authOptions.Value);
    }

    private async Task RecordFailedLoginAsync(User user, AuthOptions options, DateTime now, CancellationToken cancellationToken)
    {
        var users = db.Users.Where(u => u.Id == user.Id);
        if (user.FailedLoginCount + 1 >= options.MaxFailedLogins)
        {
            var lockoutEnd = now.AddMinutes(options.LockoutMinutes);
            await users.ExecuteUpdateAsync(
                s => s.SetProperty(u => u.FailedLoginCount, 0).SetProperty(u => u.LockoutEndUtc, lockoutEnd),
                cancellationToken);
            throw Locked(lockoutEnd);
        }

        await users.ExecuteUpdateAsync(
            s => s.SetProperty(u => u.FailedLoginCount, u => u.FailedLoginCount + 1),
            cancellationToken);
        throw InvalidCredentials();
    }

    private async Task SignInAsync(User user, bool rememberMe, AuthOptions options)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, user.LoginId),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(AuthConstants.SecurityStampClaim, user.SecurityStamp),
            ],
            AuthConstants.Scheme);

        var properties = new AuthenticationProperties { IsPersistent = rememberMe, AllowRefresh = true };
        if (rememberMe)
        {
            properties.ExpiresUtc = timeProvider.GetUtcNow().AddDays(options.RememberMeDays);
        }

        await HttpContext.SignInAsync(AuthConstants.Scheme, new ClaimsPrincipal(identity), properties);
    }

    private static UnauthorizedException InvalidCredentials() =>
        new(ErrorCodes.InvalidCredentials, "Sign-in failed", "The ID or password is incorrect.");

    private static UnauthorizedException Locked(DateTime lockoutEndUtc) =>
        new(
            ErrorCodes.AccountLocked,
            "Account locked",
            $"Too many failed sign-in attempts. Try again after {Gst.FromUtc(lockoutEndUtc):HH:mm} (GST).");
}
