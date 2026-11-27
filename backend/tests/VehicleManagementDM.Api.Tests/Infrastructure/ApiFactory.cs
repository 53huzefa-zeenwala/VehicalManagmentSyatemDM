using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Database;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Tests.Infrastructure;

/// <summary>
/// Hosts the real API in memory against a throwaway database (created by the API's own startup
/// migrations, dropped afterwards) and a temporary folder for files, logs and keys.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminPassword = "Admin-Test-Pass-1";
    public const string DefaultPassword = "Staff-Test-Pass-1";

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    public string DatabaseName { get; } = TestDatabase.NewName();

    public string TempRoot { get; } = Path.Combine(Path.GetTempPath(), "vms-tests", Guid.NewGuid().ToString("N"));

    public string FallbackLogDirectory => Path.Combine(TempRoot, "logs");

    public string FilesRoot => Path.Combine(TempRoot, "files");

    protected virtual string ConnectionString => TestDatabase.ConnectionStringFor(DatabaseName);

    protected virtual bool CreatesDatabase => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ConnectionString,
            ["Errors:IncludeExceptionDetails"] = "true",
            ["ErrorLogging:FallbackDirectory"] = FallbackLogDirectory,
            ["Storage:RootPath"] = FilesRoot,
            ["DataProtection:KeysPath"] = Path.Combine(TempRoot, "keys"),
            ["Auth:LoginAttemptsPerMinute"] = "10000",
            ["Swagger:Enabled"] = "false",
            ["Database:StartupWaitSeconds"] = "1",
            ["Seed:FirstAdmin:FullName"] = "Test Admin",
            ["Seed:FirstAdmin:Mobile"] = "+971500000001",
            ["Seed:FirstAdmin:Password"] = AdminPassword,
        }));
        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly));
    }

    public virtual async Task InitializeAsync()
    {
        _ = Server; // starts the host, which starts the migration background service
        if (CreatesDatabase)
        {
            using var timeout = new CancellationTokenSource(StartupTimeout);
            await Services.GetRequiredService<DatabaseReadiness>().WaitAsync(timeout.Token);
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (CreatesDatabase)
        {
            await TestDatabase.DropAsync(DatabaseName);
        }

        try
        {
            Directory.Delete(TempRoot, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    /// <summary>
    /// A second host on the same test database with some settings overridden; ready to use when returned.
    /// The caller disposes it.
    /// </summary>
    public async Task<WebApplicationFactory<Program>> WithSettingsAsync(Dictionary<string, string?> settings)
    {
        var derived = WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings)));
        using var timeout = new CancellationTokenSource(StartupTimeout);
        await derived.Services.GetRequiredService<DatabaseReadiness>().WaitAsync(timeout.Token);
        return derived;
    }

    /// <summary>A client that keeps cookies and talks HTTPS, so the Secure session cookie is sent back.</summary>
    public HttpClient CreateApiClient(WebApplicationFactory<Program>? factory = null) =>
        (factory ?? this).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    /// <summary>Runs code against the test database with the API's own services.</summary>
    public async Task<T> WithDbAsync<T>(Func<AppDbContext, IServiceProvider, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>(), scope.ServiceProvider);
    }

    public Task WithDbAsync(Func<AppDbContext, Task> action) =>
        WithDbAsync<bool>(async (db, _) =>
        {
            await action(db);
            return true;
        });

    /// <summary>Creates an account directly in the database (account endpoints arrive in M2).</summary>
    public Task<User> CreateUserAsync(UserRole role, string password = DefaultPassword, bool isActive = true) =>
        WithDbAsync(async (db, services) =>
        {
            var user = new User
            {
                Role = role,
                LoginId = await services.GetRequiredService<LoginIdGenerator>().NextAsync(role, CancellationToken.None),
                FullName = $"Test {role}",
                Mobile = "+971500000002",
                LicenceNumber = role == UserRole.Staff ? "LIC-TEST" : null,
                IsActive = isActive,
                Password = password,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        });
}

/// <summary>All database-backed API tests share one host and one throwaway database.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
