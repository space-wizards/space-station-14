#nullable enable
using Content.Shared.Damage.Components;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    public interface IThresholdTrigger
    {
        /// <summary>
        ///     Checks if this trigger has been reached.
        /// </summary>
        /// <param name="damageable">The damageable component to check with.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to pull
        ///     dependencies from, if any.
        /// </param>
        /// <returns>true if this trigger has been reached, false otherwise.</returns>
        bool Reached(IDamageableComponent damageable, DestructibleSystem system);
    }
}
