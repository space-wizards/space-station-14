using System;
using System.Collections.Generic;

namespace Content.Server.DamageSystem
{
    /// <summary>
    ///     Component interface that gets triggered after the values of an <see cref="IDamageableComponent"/> on the same IEntity change.
    /// </summary>
    interface IOnHealthChangedBehavior
    {
        /// <summary>
        ///     Called when the entity's <see cref="IDamageableComponent"/> is healed or hurt. Of note is that a "deal 0 damage" call will still trigger
        ///     this function (including both damage negated by resistance or simply inputting 0 as the amount of damage to deal).
        /// </summary>
        /// <param name="obj"><see cref="IDamageableComponent"/> that triggered this function.</param>
        /// <param name="e">Details of how the health changed.</param>
        public abstract void OnHealthChanged(HealthChangedEventArgs e);
    }
}
