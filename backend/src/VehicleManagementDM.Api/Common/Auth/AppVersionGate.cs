using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Common.Auth;

/// <summary>
/// Minimum app versions (D-058): each app sends its installed version in <c>X-App-Version</c>; an app older than
/// the minimum an admin set for it gets <c>426 UPDATE_REQUIRED</c> on every request and at sign-in. The app is
/// identified by the signed-in role (the staff app only signs in staff, the admin app only admins), so APKs that
/// were handed out before this check existed are covered too. The minimums are read from the settings row and
/// cached briefly; saving the settings refreshes the cache at once.
/// </summary>
public sealed class AppVersionGate(IServiceScopeFactory scopeFactory, TimeProvider timeProvider)
{
    public const string Header = "X-App-Version";
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ConcurrentDictionary<int, string> _recorded = new();
    private (Version? Staff, Version? Admin)? _minimums;
    private DateTimeOffset _loadedAt;

    /// <summary>"1.0.2", "1.0.2-local" or "1.0.2+2003" → 1.0.2; anything else → null.</summary>
    public static Version? Parse(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        var end = text.IndexOfAny(['-', '+', ' ']);
        if (end >= 0)
        {
            text = text[..end];
        }

        return Version.TryParse(text, out var version) ? Normalize(version) : null;
    }

    /// <summary>Throws 426 UPDATE_REQUIRED when <paramref name="clientVersion"/> is below the minimum for the role.</summary>
    public async Task EnsureSupportedAsync(UserRole role, string? clientVersion, CancellationToken cancellationToken)
    {
        var (staff, admin) = await GetMinimumsAsync(cancellationToken);
        var minimum = role == UserRole.Admin ? admin : staff;
        if (minimum is null)
        {
            return;
        }

        var version = Parse(clientVersion);
        if (version is null || version < minimum)
        {
            var app = role == UserRole.Admin ? "DM Admin" : "DM Staff";
            throw new AppException(
                StatusCodes.Status426UpgradeRequired,
                ErrorCodes.UpdateRequired,
                "Update required",
                $"This version of {app} ({(version is null ? "unknown" : Format(version))}) is no longer supported. " +
                $"Install version {Format(minimum)} or newer from your admin.");
        }
    }

    /// <summary>
    /// Remembers the version a person's app reported (UserAppVersions), so admins pick minimums from versions in use.
    /// Writes only when the version differs from the last one seen by this server process.
    /// </summary>
    public async Task RecordAsync(int userId, string? clientVersion, CancellationToken cancellationToken)
    {
        if (Parse(clientVersion) is not { } parsed)
        {
            return;
        }

        var version = Format(parsed);
        if (_recorded.TryGetValue(userId, out var known) && known == version)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var updated = await db.UserAppVersions.Where(v => v.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Version, version).SetProperty(v => v.SeenAtUtc, now), cancellationToken);
        if (updated == 0)
        {
            db.UserAppVersions.Add(new UserAppVersion { UserId = userId, Version = version, SeenAtUtc = now });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A parallel request inserted it first; the next request updates it if needed.
            }
        }

        _recorded[userId] = version;
    }

    /// <summary>Call after the settings change so the new minimums apply immediately.</summary>
    public void Invalidate() => _minimums = null;

    public static string Format(Version version) => $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";

    private static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0));

    private async Task<(Version? Staff, Version? Admin)> GetMinimumsAsync(CancellationToken cancellationToken)
    {
        if (_minimums is { } cached && timeProvider.GetUtcNow() - _loadedAt < CacheFor)
        {
            return cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_minimums is { } fresh && timeProvider.GetUtcNow() - _loadedAt < CacheFor)
            {
                return fresh;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var settings = await db.AppSettings.AsNoTracking()
                .Where(s => s.Id == AppSetting.SingletonId)
                .Select(s => new { s.MinStaffAppVersion, s.MinAdminAppVersion })
                .SingleAsync(cancellationToken);
            var loaded = (Parse(settings.MinStaffAppVersion), Parse(settings.MinAdminAppVersion));
            _minimums = loaded;
            _loadedAt = timeProvider.GetUtcNow();
            return loaded;
        }
        finally
        {
            _lock.Release();
        }
    }
}

/// <summary>
/// Applies <see cref="AppVersionGate"/> to signed-in API requests. Files (images load without the header),
/// health and sign-out stay open so an outdated app can still show pictures and let the person sign out.
/// </summary>
public sealed class AppVersionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppVersionGate gate)
    {
        var path = context.Request.Path;
        if (context.User.Identity?.IsAuthenticated == true
            && path.StartsWithSegments("/api/v1")
            && !path.StartsWithSegments("/api/v1/files")
            && !path.StartsWithSegments("/api/v1/auth/logout"))
        {
            var role = context.User.IsInRole(Roles.Admin) ? UserRole.Admin : UserRole.Staff;
            var version = context.Request.Headers[AppVersionGate.Header].ToString();
            await gate.EnsureSupportedAsync(role, version, context.RequestAborted);
            await gate.RecordAsync(context.User.GetUserId(), version, context.RequestAborted);
        }

        await next(context);
    }
}
