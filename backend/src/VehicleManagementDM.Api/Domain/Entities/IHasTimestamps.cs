namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>Entities whose creation and last-update times are set automatically on save.</summary>
public interface IHasTimestamps
{
    DateTime CreatedAtUtc { get; set; }

    DateTime UpdatedAtUtc { get; set; }
}
