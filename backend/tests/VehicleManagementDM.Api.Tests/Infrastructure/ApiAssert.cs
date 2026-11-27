using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace VehicleManagementDM.Api.Tests.Infrastructure;

/// <summary>The error body as the app sees it.</summary>
public sealed record ErrorBody(
    int Status,
    string Code,
    string Title,
    string Detail,
    Dictionary<string, string[]>? Errors,
    string TraceId,
    string? ExceptionType);

public static class ApiAssert
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    /// <summary>Checks the response is the one JSON error format (D-032) with the expected status and code.</summary>
    public static async Task<ErrorBody> ErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var raw = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {raw}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = JsonSerializer.Deserialize<ErrorBody>(raw, JsonOptions);
        Assert.NotNull(body);
        Assert.Equal((int)status, body.Status);
        Assert.Equal(code, body.Code);
        Assert.False(string.IsNullOrWhiteSpace(body.Title));
        Assert.False(string.IsNullOrWhiteSpace(body.Detail));
        Assert.False(string.IsNullOrWhiteSpace(body.TraceId));
        return body;
    }

    /// <summary>Checks that the error was stored in the ErrorLogs table under the trace ID sent to the app.</summary>
    public static Task<Domain.Entities.ErrorLog> LoggedAsync(ApiFactory factory, ErrorBody body) =>
        factory.WithDbAsync(async (db, _) =>
        {
            var entry = await db.ErrorLogs.AsNoTracking().SingleOrDefaultAsync(e => e.TraceId == body.TraceId);
            Assert.True(entry is not null, $"No ErrorLogs row for trace {body.TraceId}");
            Assert.Equal(body.Status, entry.StatusCode);
            Assert.Equal(body.Code, entry.Code);
            return entry;
        });

    public static async Task<T> OkAsync<T>(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Expected success, got {(int)response.StatusCode}: {raw}");
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }
}
