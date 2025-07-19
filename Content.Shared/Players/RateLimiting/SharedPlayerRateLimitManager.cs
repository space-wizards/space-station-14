using Robust.Shared.Player;

namespace Content.Shared.Players.RateLimiting;

/// <summary>
/// General-purpose system to rate limit actions taken by clients, such as chat messages.
/// </summary>
/// <remarks>
/// <para>
/// Different categories of rate limits must be registered ahead of time by calling <see cref="Register"/>.
/// Once registered, you can simply call <see cref="CountAction"/> to count a rate-limited action for a player.
/// </para>
/// <para>
/// This system is intended for rate limiting player actions over short periods,
/// to ward against spam that can cause technical issues such as admin client load.
/// It should not be used for in-game actions or similar.
/// </para>
/// <para>
/// Rate limits are reset when a client reconnects.
/// This should not be an issue for the reasonably short rate limit periods this system is intended for.
/// </para>
/// </remarks>
/// <seealso cref="RateLimitRegistration"/>
public abstract class SharedPlayerRateLimitManager
{
    /// <summary>
    /// Count and validate an action performed by a player against rate limits.
    /// </summary>
    /// <param name="player">The player performing the action.</param>
    /// <param name="key">The key string that was previously used to register a rate limit category.</param>
    /// <returns>Whether the action counted should be blocked due to surpassing rate limits or not.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="player"/> is not a connected player
    /// OR <paramref name="key"/> is not a registered rate limit category.
    /// </exception>
    /// <seealso cref="Register"/>
    public abstract RateLimitStatus CountAction(ICommonSession player, string key);

    /// <summary>
    /// Register a new rate limit category.
    /// </summary>
    /// <param name="key">
    /// The key string that will be referred to later with <see cref="CountAction"/>.
    /// Must be unique and should probably just be a constant somewhere.
    /// </param>
    /// <param name="registration">The data specifying the rate limit's parameters.</param>
    /// <exception cref="InvalidOperationException"><paramref name="key"/> has already been registered.</exception>
    /// <exception cref="ArgumentException"><paramref name="registration"/> is invalid.</exception>
    public abstract void Register(string key, RateLimitRegistration registration);

    /// <summary>
    /// Initialize the manager's functionality at game startup.
    /// </summary>
    public abstract void Initialize();
}
