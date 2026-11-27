using System.ComponentModel.DataAnnotations;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Trips.Dtos;

namespace VehicleManagementDM.Api.Features.Auth.Dtos;

/// <summary>Password rules shared by every endpoint that sets a password.</summary>
public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;
    public const string LengthMessage = "Password must be 8 to 128 characters.";
}

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Enter your ID.")]
    [StringLength(20, ErrorMessage = "The ID is too long.")]
    public string LoginId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [StringLength(PasswordRules.MaxLength, ErrorMessage = "The password is too long.")]
    public string Password { get; init; } = string.Empty;

    /// <summary>Keeps the session for 30 days instead of 12 hours (AUTH-02).</summary>
    public bool RememberMe { get; init; }
}

public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "Enter your current password.")]
    [StringLength(PasswordRules.MaxLength, ErrorMessage = "The password is too long.")]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter a new password.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.LengthMessage)]
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// The signed-in user (API_DOCUMENTATION.md §4.1). For staff: whether a trip is running, and this GST
/// month's trips and hours (Profile, Home). Admins get <c>hasActiveTrip = false</c> and no stats.
/// Staff have no assigned lots (D-047).
/// </summary>
public sealed record MeResponse(
    int Id,
    string LoginId,
    UserRole Role,
    string FullName,
    string Mobile,
    string? Email,
    string? LicenceNumber,
    string? PhotoUrl,
    bool HasActiveTrip = false,
    MonthStats? ThisMonth = null);
