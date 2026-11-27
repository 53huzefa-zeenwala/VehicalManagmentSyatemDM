using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;

namespace VehicleManagementDM.Api.Common.Validation;

/// <summary>Phone numbers: optional "+", then 7 to 15 digits (E.164 length), e.g. +971501234567.</summary>
public sealed class PhoneNumberAttribute : RegularExpressionAttribute
{
    public PhoneNumberAttribute()
        : base(@"^\+?[0-9]{7,15}$")
    {
        ErrorMessage = "Enter a valid phone number, e.g. +971501234567.";
    }
}

/// <summary>Times of day as "HH:mm" (24-hour), e.g. "06:00".</summary>
public sealed class TimeOfDayAttribute : RegularExpressionAttribute
{
    public TimeOfDayAttribute()
        : base("^([01][0-9]|2[0-3]):[0-5][0-9]$")
    {
        ErrorMessage = "Enter a time as HH:mm, e.g. 06:00.";
    }
}

public static partial class Text
{
    /// <summary>Trims and collapses inner whitespace, so "  Al   Qusais " becomes "Al Qusais".</summary>
    public static string Clean(string value) => Whitespace().Replace(value.Trim(), " ");

    /// <summary>Like <see cref="Clean"/>, but empty becomes null.</summary>
    public static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Clean(value);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

public static class TimeOfDay
{
    public static TimeOnly? Parse(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);

    public static string? Format(TimeOnly? value) =>
        value?.ToString("HH:mm", CultureInfo.InvariantCulture);
}

public static class ConcurrencyExtensions
{
    /// <summary>
    /// Makes the next save fail with 409 CONCURRENCY_CONFLICT if the row changed since the client read it
    /// (the client sends back the <c>rowVersion</c> it received).
    /// </summary>
    public static void ExpectRowVersion<T>(this DbContext db, T entity, byte[]? rowVersion)
        where T : class
    {
        if (rowVersion is not { Length: > 0 })
        {
            throw BadRequestException.ForField("rowVersion", "rowVersion is required.");
        }

        db.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
    }
}
