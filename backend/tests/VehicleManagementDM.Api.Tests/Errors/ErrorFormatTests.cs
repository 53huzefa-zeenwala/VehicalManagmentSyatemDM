using System.Net;
using System.Text;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Errors;

/// <summary>Every kind of error comes back in the one JSON format and is logged (D-032, ARCHITECTURE.md §5).</summary>
[Collection(ApiCollection.Name)]
public sealed class ErrorFormatTests(ApiFactory factory)
{
    [Fact]
    public async Task Unknown_route_for_signed_in_user_is_ROUTE_NOT_FOUND()
    {
        var client = await factory.StaffClientAsync();
        var response = await client.GetAsync("/api/v1/no-such-endpoint");

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.RouteNotFound);
        await ApiAssert.LoggedAsync(factory, body);
    }

    [Fact]
    public async Task Unknown_route_for_signed_out_caller_is_also_ROUTE_NOT_FOUND()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/v1/no-such-endpoint");

        await ApiAssert.ErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.RouteNotFound);
    }

    [Fact]
    public async Task Wrong_method_for_signed_out_caller_is_METHOD_NOT_ALLOWED()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/v1/auth/logout");

        await ApiAssert.ErrorAsync(response, HttpStatusCode.MethodNotAllowed, ErrorCodes.MethodNotAllowed);
    }

    [Fact]
    public async Task Existing_endpoint_for_signed_out_caller_is_still_UNAUTHENTICATED()
    {
        var response = await factory.CreateApiClient().PostAsync("/api/v1/auth/logout", null);

        await ApiAssert.ErrorAsync(response, HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Missing_record_is_NOT_FOUND()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/v1/test/not-found");

        await ApiAssert.ErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Wrong_method_is_METHOD_NOT_ALLOWED()
    {
        var client = await factory.StaffClientAsync();
        var response = await client.GetAsync("/api/v1/auth/login");

        await ApiAssert.ErrorAsync(response, HttpStatusCode.MethodNotAllowed, ErrorCodes.MethodNotAllowed);
    }

    [Fact]
    public async Task Invalid_fields_are_VALIDATION_FAILED_with_camelCase_field_errors()
    {
        var response = await factory.CreateApiClient().PostAsync(
            "/api/v1/test/echo",
            new StringContent("""{"age": 500}""", Encoding.UTF8, "application/json"));

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
        Assert.NotNull(body.Errors);
        Assert.Equal(["Name is required."], body.Errors["name"]);
        Assert.Equal(["Age must be between 1 and 120."], body.Errors["age"]);
        await ApiAssert.LoggedAsync(factory, body);
    }

    [Fact]
    public async Task Broken_json_is_MALFORMED_REQUEST()
    {
        var response = await factory.CreateApiClient().PostAsync(
            "/api/v1/test/echo",
            new StringContent("""{"name": "x", "age": "not a number"}""", Encoding.UTF8, "application/json"));

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.MalformedRequest);
        Assert.NotNull(body.Errors);
        Assert.Contains("age", body.Errors.Keys);
    }

    [Fact]
    public async Task Non_json_body_is_UNSUPPORTED_MEDIA_TYPE()
    {
        var response = await factory.CreateApiClient().PostAsync(
            "/api/v1/auth/login",
            new StringContent("loginId=x", Encoding.UTF8, "text/plain"));

        await ApiAssert.ErrorAsync(response, HttpStatusCode.UnsupportedMediaType, ErrorCodes.UnsupportedMediaType);
    }

    [Fact]
    public async Task Unexpected_exception_shows_real_detail_when_details_are_enabled()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/v1/test/throw");

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.InternalError);
        Assert.Equal(TestEndpointsController.SecretDetail, body.Detail);
        Assert.Equal(typeof(InvalidOperationException).FullName, body.ExceptionType);

        var entry = await ApiAssert.LoggedAsync(factory, body);
        Assert.Equal("Error", entry.Level);
        Assert.False(string.IsNullOrEmpty(entry.StackTrace));
    }

    [Fact]
    public async Task Unexpected_exception_hides_detail_in_production_mode()
    {
        await using var production = await factory.WithSettingsAsync(new() { ["Errors:IncludeExceptionDetails"] = "false" });
        var response = await factory.CreateApiClient(production).GetAsync("/api/v1/test/throw");

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.InternalError);
        Assert.StartsWith("Something went wrong on the server. Ref:", body.Detail);
        Assert.DoesNotContain(TestEndpointsController.SecretDetail, body.Detail);
        Assert.Null(body.ExceptionType);
    }

    [Fact]
    public async Task Duplicate_unique_value_is_DUPLICATE_VALUE_with_field()
    {
        var response = await factory.CreateApiClient().PostAsync("/api/v1/test/duplicate-login", null);

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.DuplicateValue);
        Assert.NotNull(body.Errors);
        Assert.Equal(["This login ID already exists."], body.Errors["loginId"]);
    }

    [Fact]
    public async Task Missing_table_is_DATABASE_SCHEMA_ERROR()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/v1/test/schema-error");

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.DatabaseSchemaError);
        await ApiAssert.LoggedAsync(factory, body);
    }

    [Fact]
    public async Task Health_is_json_and_healthy()
    {
        var response = await factory.CreateApiClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"database\":\"Healthy\"", await response.Content.ReadAsStringAsync());
    }
}
