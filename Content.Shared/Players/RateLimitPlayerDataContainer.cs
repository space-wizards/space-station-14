namespace Content.Shared.Players;

public sealed class RateLimitPlayerDataContainer
{
    /// <summary>
    /// Tracks how many github issues in the player's budget have been used. Goes up when issue creation is requested, goes
    /// down each rate-limiting interval. If this number is too high, messages cannot be sent.
    /// </summary>
    public int ActionRateOverTime;

    /// <summary>
    /// The time that the current frame-of-reference for chat spam detection expires at. This is increased set by
    /// the value of the cvar chat.rate_limit_period when the first message is sent after the last period expired.
    /// </summary>
    public TimeSpan ActionCountExpiresAt;

    /// <summary>
    /// Whether rate limiting has been announced to the player.
    /// </summary>
    public bool RateLimitAnnouncedToPlayer;

    /// <summary>
    /// When an announcement to admins can next be sent at.
    /// </summary>
    public TimeSpan CanAnnounceToAdminsNextAt;
}
