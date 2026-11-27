using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Tests.Infrastructure;

public static class AuthHelpers
{
    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string loginId, string password, bool rememberMe = false) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { loginId, password, rememberMe });

    /// <summary>A signed-in client for the seeded first admin.</summary>
    public static async Task<HttpClient> AdminClientAsync(this ApiFactory factory)
    {
        var loginId = await factory.WithDbAsync((db, _) =>
            db.Users.AsNoTracking().Where(u => u.Role == UserRole.Admin).OrderBy(u => u.Id).Select(u => u.LoginId).FirstAsync());
        var client = factory.CreateApiClient();
        (await client.LoginAsync(loginId, ApiFactory.AdminPassword)).EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>A signed-in client for a new staff account.</summary>
    public static async Task<HttpClient> StaffClientAsync(this ApiFactory factory)
    {
        var staff = await factory.CreateUserAsync(UserRole.Staff);
        var client = factory.CreateApiClient();
        (await client.LoginAsync(staff.LoginId, ApiFactory.DefaultPassword)).EnsureSuccessStatusCode();
        return client;
    }
}
