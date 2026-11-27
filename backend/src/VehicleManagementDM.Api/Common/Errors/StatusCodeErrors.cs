namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>
/// Errors for responses that end with a status code but no body (unknown route, wrong method,
/// unsupported content type…). Used by the status-code-pages middleware (ARCHITECTURE.md §5.3).
/// </summary>
public static class StatusCodeErrors
{
    public static ApiError For(HttpContext context)
    {
        var status = context.Response.StatusCode;
        var request = context.Request;

        return status switch
        {
            StatusCodes.Status401Unauthorized => Create(status, ErrorCodes.Unauthenticated, "Sign in required", "Your session has ended. Please sign in again."),
            StatusCodes.Status403Forbidden => Create(status, ErrorCodes.Forbidden, "Not allowed", "You don't have permission to do this."),
            StatusCodes.Status404NotFound when context.GetEndpoint() is null =>
                Create(status, ErrorCodes.RouteNotFound, "Not found", $"There is no API endpoint {request.Method} {request.Path}."),
            StatusCodes.Status404NotFound => Create(status, ErrorCodes.NotFound, "Not found", "The requested record was not found."),
            StatusCodes.Status405MethodNotAllowed =>
                Create(status, ErrorCodes.MethodNotAllowed, "Method not allowed", $"{request.Method} isn't allowed on {request.Path}."),
            StatusCodes.Status413PayloadTooLarge => Create(status, ErrorCodes.PayloadTooLarge, "Too large", "The request body is larger than this endpoint allows."),
            StatusCodes.Status415UnsupportedMediaType =>
                Create(status, ErrorCodes.UnsupportedMediaType, "Unsupported format", "Send the request body as JSON (Content-Type: application/json)."),
            StatusCodes.Status429TooManyRequests => Create(status, ErrorCodes.TooManyRequests, "Too many requests", "Too many attempts. Wait a minute and try again."),
            >= 500 => Create(status, ErrorCodes.InternalError, "Server error", $"Something went wrong on the server. Ref: {ApiErrorWriter.TraceIdOf(context)}"),
            _ => Create(status, ErrorCodes.RequestFailed, "Request failed", $"The request failed with status {status}."),
        };
    }

    private static ApiError Create(int status, string code, string title, string detail) =>
        new() { Status = status, Code = code, Title = title, Detail = detail };
}
