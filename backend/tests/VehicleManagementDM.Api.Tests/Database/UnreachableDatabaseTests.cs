using System.Net;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Database;

/// <summary>An API whose database can't be reached on purpose (nothing listens on that port).</summary>
public sealed class UnreachableDatabaseFactory : ApiFactory
{
    protected override string ConnectionString =>
        "Server=tcp:127.0.0.1,1;Database=VehicleManagementDM_Unreachable;User ID=nobody;Password=nothing;Connect Timeout=2;TrustServerCertificate=True";

    protected override bool CreatesDatabase => false;
}

/// <summary>With the database down the API still starts and answers in JSON (ARCHITECTURE.md §3, §5.3).</summary>
public sealed class UnreachableDatabaseTests(UnreachableDatabaseFactory factory) : IClassFixture<UnreachableDatabaseFactory>
{
    [Fact]
    public async Task Api_requests_get_DATABASE_NOT_READY_and_the_error_goes_to_the_fallback_file()
    {
        var response = await factory.CreateApiClient().PostAsync(
            "/api/v1/auth/login",
            JsonContent("""{"loginId":"DM2600001","password":"whatever-pass"}"""));

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.ServiceUnavailable, ErrorCodes.DatabaseNotReady);
        Assert.Equal("30", response.Headers.RetryAfter?.ToString());

        var logged = Directory.EnumerateFiles(factory.FallbackLogDirectory, "errors-*.jsonl")
            .SelectMany(File.ReadLines)
            .Any(line => line.Contains(body.TraceId, StringComparison.Ordinal));
        Assert.True(logged, "The error was not written to the fallback log file.");
    }

    [Fact]
    public async Task Health_reports_the_database_as_unhealthy_in_json()
    {
        var response = await factory.CreateApiClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"database\":\"Unhealthy\"", await response.Content.ReadAsStringAsync());
    }

    private static StringContent JsonContent(string json) => new(json, System.Text.Encoding.UTF8, "application/json");
}
