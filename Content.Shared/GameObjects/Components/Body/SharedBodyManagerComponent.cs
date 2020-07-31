using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body
{
    public abstract class SharedBodyManagerComponent : Component, IDamageableComponent
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

        public void OnHealthChanged(HealthChangedEventArgs e)
        {
            HealthChangedEvent?.Invoke(e);
        }
    }
}
