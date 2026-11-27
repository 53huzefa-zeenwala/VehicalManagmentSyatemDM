using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using VehicleManagementDM.Api.Common.Database;

namespace VehicleManagementDM.Api.Tests.Database;

/// <summary>Requests that arrive while the app is starting wait briefly for the database (cold start under IIS).</summary>
public sealed class DatabaseReadyMiddlewareTests
{
    [Fact]
    public async Task A_request_during_startup_waits_and_then_goes_through()
    {
        var readiness = new DatabaseReadiness();
        var passedOn = false;
        var middleware = new DatabaseReadyMiddleware(_ => { passedOn = true; return Task.CompletedTask; }, readiness, Config("10"));
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/auth/me";

        var request = middleware.InvokeAsync(context, writer: null!);
        await Task.Delay(200);
        Assert.False(request.IsCompleted);

        readiness.MarkReady();
        await request.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(passedOn);
    }

    private static IConfiguration Config(string seconds) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Database:StartupWaitSeconds"] = seconds }).Build();
}
