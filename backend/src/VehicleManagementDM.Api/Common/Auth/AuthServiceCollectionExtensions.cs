using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Options;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Common.Auth;

public static class AuthServiceCollectionExtensions
{
    /// <summary>Cookie authentication, authorization, Data Protection, password hashing and login rate limiting (D-031).</summary>
    public static IServiceCollection AddAppAuth(this IServiceCollection services)
    {
        // All settings are read through the options system when first needed, never while
        // registering services, so every configuration source (including overrides) applies.
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);

        services.AddScoped<SessionCookieEvents>();
        services
            .AddAuthentication(AuthConstants.Scheme)
            .AddCookie(AuthConstants.Scheme, options =>
            {
                options.Cookie.Name = AuthConstants.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.IsEssential = true;
                options.SlidingExpiration = true;
                options.EventsType = typeof(SessionCookieEvents);
            });
        services.AddOptions<CookieAuthenticationOptions>(AuthConstants.Scheme)
            .Configure<IOptions<AuthOptions>>((cookie, auth) => cookie.ExpireTimeSpan = TimeSpan.FromHours(auth.Value.SessionHours));

        // Sign-in is required on every controller endpoint (see MapControllers().RequireAuthorization()
        // in Program.cs). Not a fallback policy: that would also apply to URLs that match no endpoint,
        // and unknown routes must answer 404 ROUTE_NOT_FOUND to signed-out callers too (D-044).
        services.AddAuthorization();

        AddDataProtection(services);

        services.AddScoped<LoginIdGenerator>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthConstants.LoginRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress ?? IPAddress.None,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = context.RequestServices.GetRequiredService<IOptions<AuthOptions>>().Value.LoginAttemptsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                return new ValueTask(context.HttpContext.RequestServices.GetRequiredService<ApiErrorWriter>().WriteAsync(
                    context.HttpContext,
                    new ApiError
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Code = ErrorCodes.TooManyRequests,
                        Title = "Too many attempts",
                        Detail = "Too many sign-in attempts from this device. Wait a minute and try again.",
                    }));
            };
        });

        return services;
    }

    /// <summary>
    /// Cookie encryption keys must survive restarts and app-pool recycles, or every user is signed out.
    /// They are kept in <c>DataProtection:KeysPath</c> and encrypted with Windows DPAPI (ARCHITECTURE.md §4).
    /// </summary>
    private static void AddDataProtection(IServiceCollection services)
    {
        services.AddDataProtection().SetApplicationName("VehicleManagementDM");
        services.AddOptions<KeyManagementOptions>()
            .Configure<IConfiguration, IHostEnvironment, ILoggerFactory>((options, configuration, environment, loggerFactory) =>
            {
                var keysPath = configuration["DataProtection:KeysPath"];
                if (string.IsNullOrWhiteSpace(keysPath))
                {
                    return;
                }

                var fullPath = Path.IsPathRooted(keysPath) ? keysPath : Path.Combine(environment.ContentRootPath, keysPath);
                options.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(fullPath), loggerFactory);
                if (OperatingSystem.IsWindows())
                {
                    options.XmlEncryptor = new DpapiXmlEncryptor(protectToLocalMachine: true, loggerFactory);
                }
            });
    }
}
