using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.DamageSystem;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.DamageSystem
{

    /// <summary>
    ///     Component that allows attached IEntities to take damage and be destroyed after a certain threshold.
    /// </summary>
    class BasicDamageableComponent : IDamageableComponent
    {
        public override string Name => "BasicDamageable";

        [ViewVariables]
        public ResistanceSet Resistances { get; private set; }

        [ViewVariables]
        public DamageContainer DamageContainer { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            ForceHealthChangedEvent(); //Just in case something activates at default health. TODO: is there a way to call this a bit later this maybe?
        }


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // TODO: YAML writing/reading.
        }

        public override bool ChangeDamage(DamageType damageType, int amount, IEntity source)
        {
            if (DamageContainer.SupportsDamageType(damageType)){
                int finalDamage = Resistances.CalculateDamage(damageType, amount);
                DamageContainer.ChangeDamageValue(damageType, finalDamage);
                List<HealthChangeData> data = new List<HealthChangeData> { new HealthChangeData(damageType, DamageContainer.GetDamageValue(damageType), finalDamage) };
                TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
                return true;
            }
            return false;
        }

        public override bool SetDamage(DamageType damageType, int newValue, IEntity source)
        {
            if (DamageContainer.SupportsDamageType(damageType))
            {
                HealthChangeData data = new HealthChangeData(damageType, newValue, newValue-DamageContainer.GetDamageValue(damageType));
                DamageContainer.SetDamageValue(damageType, newValue);
                List<HealthChangeData> dataList = new List<HealthChangeData> { data };
                TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, dataList));
                return true;
            }
            return false;
        }

        public override void HealAllDamage()
        {
            List<HealthChangeData> data = new List<HealthChangeData>();
            foreach (DamageType type in DamageContainer.SupportedDamageTypes)
            {
                data.Add(new HealthChangeData(type, 0, -DamageContainer.GetDamageValue(type)));
                DamageContainer.SetDamageValue(type, 0);
            }
            TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
        }

        /// <summary>
        ///     Invokes the HealthChangedEvent with the current values of health. 
        /// </summary>
        public void ForceHealthChangedEvent() {
            List<HealthChangeData> data = new List<HealthChangeData>();
            foreach (DamageType type in DamageContainer.SupportedDamageTypes)
            {
                data.Add(new HealthChangeData(type, DamageContainer.GetDamageValue(type), 0));
            }
            TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
        }
    }
}
