namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>
/// Entities whose changes by admins are recorded in the audit log (AUD-01). The audit interceptor
/// writes one <see cref="AuditLog"/> row per added or changed entity in the same transaction.
/// </summary>
public interface IAuditable
{
}
