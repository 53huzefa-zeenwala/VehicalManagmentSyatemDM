using System.ComponentModel.DataAnnotations;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Features.Auth.Dtos;

namespace VehicleManagementDM.Api.Features.Accounts;

/// <summary>Fields shared by staff and admin accounts.</summary>
public class AccountRequest
{
    [Required(ErrorMessage = "Enter the full name.")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters.")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter a mobile number.")]
    [PhoneNumber]
    public string Mobile { get; init; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256, ErrorMessage = "The email can have at most 256 characters.")]
    public string? Email { get; init; }
}

/// <summary>Staff are the drivers (D-020) and aren't tied to any lot (D-047).</summary>
public class StaffRequest : AccountRequest
{
    [Required(ErrorMessage = "Enter the driving licence number.")]
    [StringLength(50, ErrorMessage = "The licence number can have at most 50 characters.")]
    public string LicenceNumber { get; init; } = string.Empty;
}

public sealed class CreateStaffRequest : StaffRequest
{
    [Required(ErrorMessage = "Enter a password.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.LengthMessage)]
    public string Password { get; init; } = string.Empty;
}

public sealed class UpdateStaffRequest : StaffRequest
{
    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

public sealed class CreateAdminRequest : AccountRequest
{
    [Required(ErrorMessage = "Enter a password.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.LengthMessage)]
    public string Password { get; init; } = string.Empty;
}

public sealed class UpdateAdminRequest : AccountRequest
{
    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

public sealed class AccountStatusRequest
{
    public bool IsActive { get; init; }

    [Required(ErrorMessage = "rowVersion is required.")]
    public byte[]? RowVersion { get; init; }
}

public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "Enter a new password.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.LengthMessage)]
    public string NewPassword { get; init; } = string.Empty;
}

public sealed class StaffQuery : PageQuery
{
    /// <summary>Matches name, Staff ID or mobile.</summary>
    [StringLength(100)]
    public string? Search { get; init; }

    public bool? IsActive { get; init; }
}

public sealed record StaffResponse(
    int Id,
    string LoginId,
    string FullName,
    string Mobile,
    string? Email,
    string LicenceNumber,
    string? PhotoUrl,
    bool IsActive,
    DateTime CreatedAtUtc,
    byte[] RowVersion);

public sealed record AdminResponse(
    int Id,
    string LoginId,
    string FullName,
    string Mobile,
    string? Email,
    bool IsActive,
    DateTime CreatedAtUtc,
    byte[] RowVersion);
