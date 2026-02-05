namespace APS.Bridge.Models;

/// <summary>
/// Internal state tracking for bridge authentication
/// </summary>
public class BridgeAuthStatus
{
    /// <summary>
    /// Whether a login flow is currently in progress
    /// </summary>
    public bool IsLoginInProgress { get; set; }

    /// <summary>
    /// Timestamp when login flow started
    /// </summary>
    public DateTime LoginStartedAt { get; set; }

    /// <summary>
    /// Login flow timeout in seconds
    /// </summary>
    public const int LoginTimeoutSeconds = 120;

    /// <summary>
    /// Whether login has timed out
    /// </summary>
    public bool IsLoginTimedOut => IsLoginInProgress &&
        DateTime.UtcNow > LoginStartedAt.AddSeconds(LoginTimeoutSeconds);
}
