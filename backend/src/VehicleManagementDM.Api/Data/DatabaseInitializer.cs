using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Database;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Logging;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Data;

/// <summary>
/// At startup: creates the database if missing, applies pending migrations (D-030), and creates the
/// first admin when there is none (AUTH-06). Runs in the background and keeps retrying while the
/// database is unreachable, so the API itself always starts and answers in JSON (ARCHITECTURE.md §3).
/// </summary>
public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    DatabaseReadiness readiness,
    FileErrorLog fileLog,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<DatabaseInitializer> logger) : BackgroundService
{
    private const int MinPasswordLength = 8;
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryDelay = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await InitializeAsync(stoppingToken);
                readiness.MarkReady();
                logger.LogInformation("Database is ready");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Database startup failed; retrying in {Delay}", retryDelay);
                await RecordFailureAsync(exception);
            }

            try
            {
                await Task.Delay(retryDelay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * 2, MaxRetryDelay.Ticks));
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Default")))
        {
            throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} database migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        }

        await db.Database.MigrateAsync(cancellationToken);
        await SeedFirstAdminAsync(scope.ServiceProvider, db, cancellationToken);
    }

    private async Task SeedFirstAdminAsync(IServiceProvider services, AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
        {
            return;
        }

        var section = configuration.GetSection("Seed:FirstAdmin");
        var fullName = section["FullName"];
        var mobile = section["Mobile"];
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No admin account exists. Set Seed:FirstAdmin:FullName, :Mobile and :Password to create the first one (AUTH-06)");
            return;
        }

        if (password.Length < MinPasswordLength)
        {
            logger.LogError("Seed:FirstAdmin:Password must have at least {Length} characters; the first admin was not created", MinPasswordLength);
            return;
        }

        var admin = new User
        {
            Role = UserRole.Admin,
            LoginId = await services.GetRequiredService<LoginIdGenerator>().NextAsync(UserRole.Admin, cancellationToken),
            FullName = fullName.Trim(),
            Mobile = mobile.Trim(),
        };
        admin.PasswordHash = services.GetRequiredService<IPasswordHasher<User>>().HashPassword(admin, password);

        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning("First admin account created with login ID {LoginId}", admin.LoginId);
    }

    private async Task RecordFailureAsync(Exception exception)
    {
        try
        {
            await fileLog.AppendAsync(new ErrorLog
            {
                AtUtc = timeProvider.GetUtcNow().UtcDateTime,
                TraceId = "startup",
                Level = "Critical",
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                Code = ErrorCodes.DatabaseNotReady,
                ExceptionType = exception.GetType().FullName,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.ToString(),
            });
        }
        catch (Exception fileException)
        {
            logger.LogCritical(fileException, "Could not write the startup failure to the fallback file");
        }
    }
}
