using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>An admin or staff account (DATABASE_DESIGN.md §3.1).</summary>
public class User : IHasTimestamps, IAuditable
{
    public int Id { get; set; }

    /// <summary>Generated login ID, stored upper-case: DM2600042 (staff) or ADM2600001 (admin).</summary>
    public string LoginId { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>Driving licence number; required for staff (D-020), null for admins.</summary>
    public string? LicenceNumber { get; set; }

    /// <summary>API-generated file name only (D-034).</summary>
    public string? PhotoFileName { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Changes on deactivation and password change or reset; ends every existing session (AUTH-07).</summary>
    public string SecurityStamp { get; set; } = NewSecurityStamp();

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutEndUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
}
