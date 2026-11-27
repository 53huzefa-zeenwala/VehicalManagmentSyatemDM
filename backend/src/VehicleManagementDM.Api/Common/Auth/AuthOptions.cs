namespace VehicleManagementDM.Api.Common.Auth;

/// <summary>Configuration section "Auth" (ARCHITECTURE.md §4).</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Session length without "remember me".</summary>
    public int SessionHours { get; set; } = 12;

    /// <summary>Session length with "remember me" (sliding, AUTH-02).</summary>
    public int RememberMeDays { get; set; } = 30;

    /// <summary>Failed logins before the account locks (AUTH-08).</summary>
    public int MaxFailedLogins { get; set; } = 5;

    /// <summary>How long a locked account stays locked (AUTH-08).</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Login attempts allowed per client IP per minute (rate limiter).</summary>
    public int LoginAttemptsPerMinute { get; set; } = 10;
}
