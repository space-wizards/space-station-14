using Content.Server.BodySystem;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Observer;
using Content.Shared.DamageSystem;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Players;
using System;
using System.Collections.Generic;

namespace Content.Server.DamageSystem
{

    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, this component allows it to take damage and manages its damage-related interactions.
    /// </summary>
    public abstract class IDamageableComponent : Component, IExAct, IRelayMoveInput
    {
        /// <summary>
        ///     Called when the entity's <see cref="IDamageableComponent"/> values change. Of note is that a "deal 0 damage" call will still trigger
        ///     this event (including both damage negated by resistance or simply inputting 0 as the amount of damage to deal).
        /// </summary>
        public event Action<HealthChangedEventArgs> HealthChangedEvent;

        /// <summary>
        ///     List of all <see cref="DamageState">DamageStates</see> that <see cref="CurrentDamageState"> can be.
        /// </summary>
        public abstract List<DamageState> SupportedDamageStates { get; }

        /// <summary>
        ///     The <see cref="DamageState"/> currently representing this component. 
        /// </summary>
        public abstract DamageState CurrentDamageState { get; protected set; }

        /// <summary>
        ///     Sum of all damages taken.
        /// </summary>
        public abstract int TotalDamage { get; }

        public override void Initialize()
        {
            base.Initialize();
            foreach (var behavior in Owner.GetAllComponents<IOnHealthChangedBehavior>())
            {
                HealthChangedEvent += behavior.OnHealthChanged;
            }
            ForceHealthChangedEvent(); //Just in case something activates at default health. TODO: is there a way to call this a bit later this maybe?
        } 

        /// <summary>
        ///     Changes the specified <see cref="DamageType"/>, applying resistance values only if it is damage. Returns false if the given damageType is not supported or improper HealthChangeParams were provided; true otherwise.
        /// </summary>
        /// <param name="damageType">Type of damage being changed.</param>
        /// <param name="amount">Amount of damage being received (positive for damage, negative for heals).</param>
        /// <param name="source">Entity that dealt or healed the damage.</param>
        /// <param name="ignoreResistances">Whether to ignore resistances. Healing always ignores resistances, regardless of this input.</param>
        /// <param name="extraParams">Extra parameters that some components may require, such as a specific limb to target.</param>
        public abstract bool ChangeDamage(DamageType damageType, int amount, IEntity source, bool ignoreResistances, HealthChangeParams extraParams = null);

        /// <summary>
        ///     Changes the specified <see cref="DamageClass"/>, applying resistance values only if it is damage. Returns false if the given damageClass is not supported or improper HealthChangeParams were provided; true otherwise.
        ///     Spreads amount evenly between the <see cref="DamageType">DamageTypes</see> represented by that class.
        /// </summary>
        /// <param name="damageType">Class of damage being changed.</param>
        /// <param name="amount">Amount of damage being received (positive for damage, negative for heals).</param>
        /// <param name="source">Entity that dealt or healed the damage.</param>
        /// <param name="ignoreResistances">Whether to ignore resistances. Healing always ignores resistances, regardless of this input.</param>
        /// <param name="extraParams">Extra parameters that some components may require, such as a specific limb to target.</param>
        public abstract bool ChangeDamage(DamageClass damageClass, int amount, IEntity source, bool ignoreResistances, HealthChangeParams extraParams = null);

        /// <summary>
        ///     Forcefully sets the specified <see cref="DamageType"/> to the given value, ignoring resistance values. Returns false if the given damageType is not supported or improper HealthChangeParams were provided; true otherwise.
        /// </summary>
        /// <param name="damageType">Type of damage being changed.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <param name="source">Entity that set the new damage value.</param>
        /// <param name="extraParams">Extra parameters that some components may require, such as a specific limb to target.</param>
        public abstract bool SetDamage(DamageType damageType, int newValue, IEntity source, HealthChangeParams extraParams = null);

        /// <summary>
        ///     Sets all damage values to zero.
        /// </summary>
        public abstract void HealAllDamage();

        /// <summary>
        ///     Invokes the HealthChangedEvent with the current values of health. 
        /// </summary>
        public abstract void ForceHealthChangedEvent();

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    ChangeDamage(DamageType.Piercing, 250, null, false);
                    ChangeDamage(DamageType.Heat, 250, null, false);
                    break;
                case ExplosionSeverity.Heavy:
                    ChangeDamage(DamageType.Piercing, 60, null, false);
                    ChangeDamage(DamageType.Heat, 60, null, false);
                    break;
                case ExplosionSeverity.Light:
                    ChangeDamage(DamageType.Piercing, 20, null, false);
                    ChangeDamage(DamageType.Heat, 20, null, false);
                    break;
            }
        }

        protected void TryInvokeHealthChangedEvent(HealthChangedEventArgs e)
        {
            HealthChangedEvent?.Invoke(e);
        }
        		
        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                new Ghost().Execute(null, (IPlayerSession) session, null);
            }
        }
    }

    /// <summary>
    ///     Data class with information on how to damage an <see cref="IDamageableComponent"/>. While not necessary to damage for all instances, classes such as
    ///     <see cref="BodyManagerComponent"/> may require it for extra data (such as selecting which limb to target).
    /// </summary>
    public class HealthChangeParams : EventArgs
    {

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
        public DamageType Type;

        /// <summary>
        ///     The new current value for that damage.
        /// </summary>
        public int NewValue;

        /// <summary>
        ///     How much the health value changed from its last value (negative is heals, positive is damage).
        /// </summary>
        public int Delta;

        public HealthChangeData(DamageType type, int newValue, int delta)
        {
            Type = type;
            NewValue = newValue;
            Delta = delta;
        }
    }
}
