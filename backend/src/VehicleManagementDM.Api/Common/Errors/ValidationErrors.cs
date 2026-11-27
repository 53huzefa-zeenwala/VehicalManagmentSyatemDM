using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>
/// Replaces MVC's default validation response: DTO errors become 400 VALIDATION_FAILED, and
/// unreadable JSON becomes 400 MALFORMED_REQUEST, both in the one JSON error format.
/// </summary>
public static class ValidationErrors
{
    public static IActionResult Respond(ActionContext context) => new ApiErrorResult(FromModelState(context.ModelState));

    public static ApiError FromModelState(ModelStateDictionary modelState)
    {
        var errors = new Dictionary<string, string[]>();
        var malformed = false;

        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            // System.Text.Json failures arrive with a "$" / "$.field" key (or an empty key for an empty body).
            var isJsonError = key.StartsWith('$') || entry.Errors.Any(e => e.Exception is not null);
            malformed |= isJsonError;

            var field = FieldName(key);
            errors[field] = entry.Errors
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message ?? "Invalid value." : e.ErrorMessage)
                .ToArray();
        }

        var first = errors.Values.SelectMany(m => m).FirstOrDefault() ?? "The request is invalid.";
        return malformed
            ? new ApiError
            {
                Status = StatusCodes.Status400BadRequest,
                Code = ErrorCodes.MalformedRequest,
                Title = "Invalid request",
                Detail = first,
                Errors = errors,
            }
            : new ApiError
            {
                Status = StatusCodes.Status400BadRequest,
                Code = ErrorCodes.ValidationFailed,
                Title = "Check the highlighted fields",
                Detail = first,
                Errors = errors,
            };
    }

    private static string FieldName(string key)
    {
        var field = key.TrimStart('$').TrimStart('.');
        return field.Length == 0 ? "body" : field;
    }
}
