using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body
{
    public abstract class SharedBodyManagerComponent : Component, IBodyManagerComponent
    {
        public sealed override string Name => "BodyManager";

        public event Action<HealthChangedEventArgs> HealthChangedEvent;

        public abstract List<DamageState> SupportedDamageStates { get; }

        public abstract DamageState CurrentDamageState { get; protected set; }

        public abstract int TotalDamage { get; }

        public abstract bool ChangeDamage(DamageType damageType, int amount, IEntity source, bool ignoreResistances,
            HealthChangeParams extraParams = null);

        public abstract bool ChangeDamage(DamageClass damageClass, int amount, IEntity source, bool ignoreResistances,
            HealthChangeParams extraParams = null);

        public abstract bool SetDamage(DamageType damageType, int newValue, IEntity source,
            HealthChangeParams extraParams = null);

        public abstract void HealAllDamage();

        public abstract void ForceHealthChangedEvent();

        protected void OnHealthChanged(HealthChangedEventArgs e)
        {
            HealthChangedEvent?.Invoke(e);
        }
    }

    /// <summary>
    ///     Used to determine whether a BodyPart can connect to another BodyPart.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartCompatibility
    {
        Universal = 0,
        Biological,
        Mechanical
    }

    /// <summary>
    ///     Each BodyPart has a BodyPartType used to determine a variety of things.
    ///     For instance, what slots it can fit into.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartType
    {
        Other = 0,
        Torso,
        Head,
        Arm,
        Hand,
        Leg,
        Foot
    }

    /// <summary>
    ///     Defines a surgery operation that can be performed.
    /// </summary>
    [Serializable, NetSerializable]
    public enum SurgeryType
    {
        None = 0,
        Incision,
        Retraction,
        Cauterization,
        VesselCompression,
        Drilling,
        Amputation
    }
}
