using System;
using System.Collections.Generic;

namespace Content.Server.DamageSystem
{
    /// <summary>
    ///     Component interface that gets triggered after the values of an <see cref="IDamageableComponent"/> change.
    /// </summary>
    interface IOnDamageBehavior
    {
        /// <summary>
        ///     Gets a list of all DamageThresholds this component/entity are interested in.
        /// </summary>
        /// <returns>List of DamageThresholds to be added to DamageableComponent for watching.</returns>
        List<DamageThreshold> GetAllDamageThresholds() => null;

        /// <summary>
        /// Damage threshold passed event hookup.
        /// </summary>
        /// <param name="obj">Damageable component.</param>
        /// <param name="e">Damage threshold and whether it's passed in one way or another.</param>
        void OnDamageThresholdPassed(object obj, DamageThresholdPassedEventArgs e) { }

        /// <summary>
        /// Called when the entity is damaged.
        /// </summary>
        /// <param name="obj">Damageable component.</param>
        /// <param name="e">DamageEventArgs</param>
        void OnDamaged(object obj, DamageEventArgs e) { }
    }
}
