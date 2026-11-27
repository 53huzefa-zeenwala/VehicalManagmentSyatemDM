using Microsoft.AspNetCore.Http;

namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>
/// A known, expected error with a real user-facing message. Services throw these; the global
/// exception handler turns them into <see cref="ApiError"/> responses (ARCHITECTURE.md §5.3).
/// </summary>
public class AppException(
    int status,
    string code,
    string title,
    string detail,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(detail)
{
    public int Status { get; } = status;

    public string Code { get; } = code;

    public string Title { get; } = title;

    public string Detail { get; } = detail;

    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}

/// <summary>400: the request breaks a validation rule found by a service (not by DataAnnotations).</summary>
public sealed class BadRequestException(string code, string detail, IReadOnlyDictionary<string, string[]>? errors = null)
    : AppException(StatusCodes.Status400BadRequest, code, "Invalid request", detail, errors)
{
    /// <summary>A single-field validation error.</summary>
    public static BadRequestException ForField(string field, string message) =>
        new(ErrorCodes.ValidationFailed, message, new Dictionary<string, string[]> { [field] = [message] });
}

/// <summary>401: credentials or session problems (login failures, locked or deactivated accounts).</summary>
public sealed class UnauthorizedException(string code, string title, string detail)
    : AppException(StatusCodes.Status401Unauthorized, code, title, detail);

/// <summary>403: logged in, but not allowed to do this.</summary>
public sealed class ForbiddenException(string detail, string code = ErrorCodes.Forbidden)
    : AppException(StatusCodes.Status403Forbidden, code, "Not allowed", detail);

/// <summary>404: the requested record doesn't exist.</summary>
public sealed class NotFoundException(string detail, string code = ErrorCodes.NotFound)
    : AppException(StatusCodes.Status404NotFound, code, "Not found", detail);

/// <summary>409: the request conflicts with the current state (business rules, duplicates).</summary>
public sealed class ConflictException(string code, string title, string detail, IReadOnlyDictionary<string, string[]>? errors = null)
    : AppException(StatusCodes.Status409Conflict, code, title, detail, errors);

/// <summary>413: an upload is bigger than allowed.</summary>
public sealed class PayloadTooLargeException(string detail)
    : AppException(StatusCodes.Status413PayloadTooLarge, ErrorCodes.PayloadTooLarge, "Too large", detail);
