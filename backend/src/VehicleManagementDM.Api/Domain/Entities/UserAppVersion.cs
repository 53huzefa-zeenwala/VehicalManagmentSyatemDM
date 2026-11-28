namespace VehicleManagementDM.Api.Domain.Entities;

/// <summary>
/// The app version a person last used (D-058), kept apart from <see cref="User"/> so recording it never changes the
/// account's concurrency token. Lets admins choose a minimum version from the versions actually in use.
/// </summary>
public class UserAppVersion
{
    public int UserId { get; set; }

    /// <summary>"major.minor.patch".</summary>
    public string Version { get; set; } = string.Empty;

    public DateTime SeenAtUtc { get; set; }

    public User? User { get; set; }
}
