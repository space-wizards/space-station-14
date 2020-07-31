using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Damage
{
    /// <summary>
    ///     Component interface that gets triggered after the values of a
    ///     <see cref="IDamageableComponent"/> on the same <see cref="IEntity"/> change.
    /// </summary>
    public interface IOnHealthChangedBehavior
    {
        /// <summary>
        ///     Called when the entity's <see cref="IDamageableComponent"/>
        ///     is healed or hurt.
        ///     Of note is that a "deal 0 damage" call will still trigger
        ///     this function (including both damage negated by resistance or
        ///     simply inputting 0 as the amount of damage to deal).
        /// </summary>
        /// <param name="e">Details of how the health changed.</param>
        public void OnHealthChanged(HealthChangedEventArgs e);
    }
}
