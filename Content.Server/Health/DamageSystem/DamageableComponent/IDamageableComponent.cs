using Content.Server.GameObjects;
using Content.Shared.DamageSystem;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;

namespace Content.Server.DamageSystem
{

    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, this component allows it to take damage and manages its damage-related interactions.
    /// </summary>
    public abstract class IDamageableComponent : Component
    {
        protected Dictionary<DamageType, List<DamageThreshold>> Thresholds = new Dictionary<DamageType, List<DamageThreshold>>();
        protected event EventHandler<DamageThresholdPassedEventArgs> DamageThresholdEvent;

        /// <summary>
        ///     Takes the specified damage. Returns false if the given damageType is not supported; true otherwise.
        /// </summary>
        /// <param name="damageType">Type of damage being received.</param>
        /// <param name="amount">Amount of damage being received (positive for damage, negative for heals).</param>
        /// <param name="source">Entity that dealt or healed the damage.</param>
        public abstract bool TakeDamage(DamageType damageType, int amount, IEntity source);

        /// <summary>
        ///     Sets the specified <see cref="DamageType"/> to the given value. Returns false if the given damageType is not supported; true otherwise.
        /// </summary>
        /// <param name="damageType">Type of damage being changed.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <param name="source">Entity that set the new damage value.</param>
        public abstract bool SetDamage(DamageType damageType, int newValue, IEntity source);

        public abstract void HealAllDamage();

        public abstract bool IsDead();

    }
}
