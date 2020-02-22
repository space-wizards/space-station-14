using System;
using Content.Server.GameObjects;
using Content.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects
{
    public interface IDamageableComponent : IComponent
    {
        event EventHandler<DamageThresholdPassedEventArgs> DamageThresholdPassed;
        ResistanceSet Resistances { get; }

        /// <summary>
        /// The function that handles receiving damage.
        /// Converts damage via the resistance set then applies it
        /// and informs components of thresholds passed as necessary.
        /// </summary>
        /// <param name="damageType">Type of damage being received.</param>
        /// <param name="amount">Amount of damage being received.</param>
        /// <param name="source">Entity that damaged this entity.</param>
        /// <param name="sourceMob">Mob that damaged this entity.</param>
        void TakeDamage(DamageType damageType, int amount, IEntity source, IEntity sourceMob);

        /// <summary>
        /// Handles receiving healing.
        /// Converts healing via the resistance set then applies it
        /// and informs components of thresholds passed as necessary.
        /// </summary>
        /// <param name="damageType">Type of healing being received.</param>
        /// <param name="amount">Amount of healing being received.</param>
        /// <param name="source">Entity that healed this entity.</param>
        /// <param name="sourceMob">Mob that healed this entity.</param>
        void TakeHealing(DamageType damageType, int amount, IEntity source, IEntity sourceMob);
    }
}
