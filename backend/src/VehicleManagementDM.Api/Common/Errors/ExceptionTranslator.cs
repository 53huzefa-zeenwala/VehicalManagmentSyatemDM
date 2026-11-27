using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>
/// Maps any exception to the API error it should produce (ARCHITECTURE.md §5.2):
/// known errors keep their real message; unexpected ones hide details unless enabled.
/// </summary>
public static partial class ExceptionTranslator
{
    // SQL Server error numbers (https://learn.microsoft.com/sql/relational-databases/errors-events/database-engine-events-and-errors)
    private static readonly HashSet<int> DuplicateKeyErrors = [2601, 2627];
    private static readonly HashSet<int> SchemaErrors = [207, 208, 2812];
    private static readonly HashSet<int> TimeoutErrors = [-2, 1205, 1222];
    private static readonly HashSet<int> ConnectionErrors =
    [
        2, 40, 53, 64, 121, 233, 4060, 10053, 10054, 10060, 10061, 11001, 18452, 18456, 40197, 40501, 40613,
    ];

    public static ApiError Translate(Exception exception, bool includeExceptionDetails, string traceId)
    {
        switch (exception)
        {
            case AppException app:
                return new ApiError
                {
                    Status = app.Status,
                    Code = app.Code,
                    Title = app.Title,
                    Detail = app.Detail,
                    Errors = app.Errors,
                };

            case BadHttpRequestException badRequest:
                return FromBadRequest(badRequest);

            case DbUpdateConcurrencyException:
                return new ApiError
                {
                    Status = StatusCodes.Status409Conflict,
                    Code = ErrorCodes.ConcurrencyConflict,
                    Title = "Changed by someone else",
                    Detail = "This record was changed by someone else. Reload and try again.",
                };
        }

        if (FindSqlException(exception) is { } sql)
        {
            return FromSql(sql, includeExceptionDetails, traceId);
        }

        if (exception is InvalidOperationException && exception.Message.Contains("connection from the pool", StringComparison.OrdinalIgnoreCase))
        {
            return DatabaseTimeout();
        }

        return new ApiError
        {
            Status = StatusCodes.Status500InternalServerError,
            Code = ErrorCodes.InternalError,
            Title = "Server error",
            Detail = includeExceptionDetails
                ? exception.Message
                : $"Something went wrong on the server. Ref: {traceId}",
            ExceptionType = includeExceptionDetails ? exception.GetType().FullName : null,
        };
    }

    private static ApiError FromBadRequest(BadHttpRequestException exception) => exception.StatusCode switch
    {
        StatusCodes.Status413PayloadTooLarge => new ApiError
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Code = ErrorCodes.PayloadTooLarge,
            Title = "Too large",
            Detail = "The request body is larger than this endpoint allows.",
        },
        _ => new ApiError
        {
            Status = exception.StatusCode,
            Code = ErrorCodes.MalformedRequest,
            Title = "Invalid request",
            Detail = exception.Message,
        },
    };

    private static ApiError FromSql(SqlException sql, bool includeExceptionDetails, string traceId)
    {
        var numbers = sql.Errors.Cast<SqlError>().Select(e => e.Number).ToList();

        if (numbers.Any(DuplicateKeyErrors.Contains))
        {
            var entry = UniqueIndexMessages.For(IndexName(sql.Message));
            return new ApiError
            {
                Status = StatusCodes.Status409Conflict,
                Code = entry.Code,
                Title = entry.Title,
                Detail = entry.Message,
                Errors = entry.Field is null ? null : new Dictionary<string, string[]> { [entry.Field] = [entry.Message] },
            };
        }

        if (numbers.Any(TimeoutErrors.Contains))
        {
            return DatabaseTimeout();
        }

        if (numbers.Any(ConnectionErrors.Contains) || sql.Class >= 20)
        {
            return new ApiError
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Code = ErrorCodes.DatabaseUnavailable,
                Title = "Database unavailable",
                Detail = "The server can't reach the database. Try again shortly.",
            };
        }

        if (numbers.Any(SchemaErrors.Contains))
        {
            return new ApiError
            {
                Status = StatusCodes.Status500InternalServerError,
                Code = ErrorCodes.DatabaseSchemaError,
                Title = "Database schema error",
                Detail = includeExceptionDetails
                    ? sql.Message
                    : $"The database structure doesn't match this version of the server. Ref: {traceId}",
                ExceptionType = includeExceptionDetails ? sql.GetType().FullName : null,
            };
        }

        return new ApiError
        {
            Status = StatusCodes.Status500InternalServerError,
            Code = ErrorCodes.InternalError,
            Title = "Server error",
            Detail = includeExceptionDetails
                ? sql.Message
                : $"Something went wrong on the server. Ref: {traceId}",
            ExceptionType = includeExceptionDetails ? sql.GetType().FullName : null,
        };
    }

    private static ApiError DatabaseTimeout() => new()
    {
        Status = StatusCodes.Status503ServiceUnavailable,
        Code = ErrorCodes.DatabaseTimeout,
        Title = "Database busy",
        Detail = "The database took too long to respond. Try again.",
    };

    private static SqlException? FindSqlException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql)
            {
                return sql;
            }
        }

        return null;
    }

    private static string? IndexName(string message)
    {
        var match = IndexNamePattern().Match(message);
        return match.Success ? match.Groups["name"].Value : null;
    }

    // "...with unique index 'UX_Users_LoginId'..." (2601) or "...UNIQUE KEY constraint 'UQ_x'..." (2627)
    [GeneratedRegex(@"(?:unique index|constraint) '(?<name>[^']+)'", RegexOptions.IgnoreCase)]
    private static partial Regex IndexNamePattern();
}
