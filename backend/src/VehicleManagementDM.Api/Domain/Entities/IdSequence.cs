using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>Last number issued for login IDs of one role. Never reset, never reused (D-011, D-037).</summary>
public class IdSequence
{
    public UserRole Role { get; set; }

    public int LastNumber { get; set; }
}
