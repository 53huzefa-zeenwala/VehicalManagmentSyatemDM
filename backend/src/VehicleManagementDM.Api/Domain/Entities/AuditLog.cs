namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>One admin change (AUD-01, DATABASE_DESIGN.md §3.13). Kept forever.</summary>
public class AuditLog
{
    public long Id { get; set; }

    public DateTime AtUtc { get; set; }

    /// <summary>The admin who made the change.</summary>
    public int UserId { get; set; }

    /// <summary>Create, Update, Deactivate, Reactivate, StatusChange, PasswordReset, LocationResolve, Delete.</summary>
    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    /// <summary>Changed fields before the change (JSON); null for creates.</summary>
    public string? BeforeJson { get; set; }

    /// <summary>Changed fields after the change (JSON); null for deletes.</summary>
    public string? AfterJson { get; set; }
}
