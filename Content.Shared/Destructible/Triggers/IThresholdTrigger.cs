using Content.Shared.Damage.Components;

namespace Content.Shared.Destructible.Thresholds.Triggers;

/// <summary>
/// A condition for triggering a <see cref="DamageThreshold">.
/// </summary>
/// <remarks>
/// I decided against converting these into EntityEffectConditions for performance reasons
/// (although I did not do any benchmarks, so it might be fine).
/// Entity effects will raise a separate event for each entity and each condition, which can become a huge number
/// for cases like nuke explosions or shuttle collisions where there are lots of DamageChangedEvents at once.
/// IThresholdTriggers on the other hand are directly checked in a foreach loop without raising events.
/// And there are only few of these conditions, so there is only a minor amount of code duplication.
/// </remarks>
public interface IThresholdTrigger
{
    /// <summary>
    /// Checks if this trigger has been reached.
    /// </summary>
    /// <param name="damageable">The damageable component to check with.</param>
    /// <param name="system">
    /// An instance of <see cref="SharedDestructibleSystem"/> to pull dependencies from, if any.
    /// </param>
    /// <returns>true if this trigger has been reached, false otherwise.</returns>
    bool Reached(Entity<DamageableComponent> damageable, SharedDestructibleSystem system);
}
