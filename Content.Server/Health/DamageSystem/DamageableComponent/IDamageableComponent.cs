
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
        /// <summary>
        ///     Called when the entity's <see cref="IDamageableComponent"/> changes. Of note is that a "deal 0 damage" call will still trigger
        ///     this event (including both damage negated by resistance or simply inputting 0 as the amount of damage to deal).
        /// </summary>
        public event Action<HealthChangedEventArgs> HealthChangedEvent;

        public override void Initialize()
        {
            base.Initialize();
            foreach (var behavior in Owner.GetAllComponents<IOnHealthChangedBehavior>())
            {
                HealthChangedEvent += behavior.OnHealthChanged;
            }
        }

        /// <summary>
        ///     Changes the specified damage, applying resistance values only if it is damage. Returns false if the given damageType is not supported; true otherwise.
        /// </summary>
        /// <param name="damageType">Type of damage being received.</param>
        /// <param name="amount">Amount of damage being received (positive for damage, negative for heals).</param>
        /// <param name="source">Entity that dealt or healed the damage.</param>
        public abstract bool ChangeDamage(DamageType damageType, int amount, IEntity source);

        /// <summary>
        ///     Forcefully sets the specified <see cref="DamageType"/> to the given value, ignoring resistance values. Returns false if the given damageType is not supported; true otherwise.
        /// </summary>
        /// <param name="damageType">Type of damage being changed.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <param name="source">Entity that set the new damage value.</param>
        public abstract bool SetDamage(DamageType damageType, int newValue, IEntity source);

        public abstract void HealAllDamage();



        protected void TryInvokeHealthChangedEvent(HealthChangedEventArgs e)
        {
            HealthChangedEvent?.Invoke(e);
        }

    }



    /// <summary>
    ///     Data class with information on how the <see cref="DamageType"/> values of a <see cref="IDamageableComponent"/> have changed.
    /// </summary>
    public class HealthChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     Reference to the <see cref="IDamageableComponent"/> that invoked the event.
        /// </summary>
        public IDamageableComponent DamageableComponent;

        /// <summary>
        ///     List containing data on each <see cref="DamageType"/> that was changed.
        /// </summary>
        public List<HealthChangeData> HealthData { get; }

        public HealthChangedEventArgs(IDamageableComponent damageableComponent, List<HealthChangeData> healthData)
        {
            DamageableComponent = damageableComponent;
            HealthData = healthData;
        }
    }

    /// <summary>
    ///     Data class with information on how the value of a single <see cref="DamageType"/> has changed.
    /// </summary>
    public struct HealthChangeData
    {
        /// <summary>
        ///     Type of damage that changed.
        /// </summary>
        public DamageType Type { get; set; }

        /// <summary>
        ///     The new current value for that damage.
        /// </summary>
        public int NewValue { get; set; }

        /// <summary>
        ///     How much the health value changed from its last value (negative is heals, positive is damage).
        /// </summary>
        public int Delta { get; set; }

        public HealthChangeData(DamageType type, int newValue, int delta)
        {
            Type = type;
            NewValue = newValue;
            Delta = delta;
        }
    }
}
