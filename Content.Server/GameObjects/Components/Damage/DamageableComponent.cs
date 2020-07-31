using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     Component that allows attached entities to take damage.
    ///     This basic version never dies (thus can take an indefinite amount of damage).
    /// </summary>
    [ComponentReference(typeof(IDamageableComponent))]
    public class DamageableComponent : Component, IDamageableComponent
    {
        public override string Name => "BasicDamageable";

        public event Action<HealthChangedEventArgs> HealthChangedEvent;

        [ViewVariables] private ResistanceSet Resistances { get; set; }

        [ViewVariables] private DamageContainer CurrentDamages { get; set; }

        public virtual List<DamageState> SupportedDamageStates => new List<DamageState> {DamageState.Alive};

        public DamageState CurrentDamageState
        {
            // Can tank infinite damage.
            get => DamageState.Alive;
            protected set { }
        }

        public int TotalDamage => CurrentDamages.TotalDamage;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                // Doesn't write to file, TODO?
                // Yes, TODO
                var damageContainerId = "biologicalDamageContainer";
                var resistanceSetId = "defaultResistances";

                serializer.DataField(ref damageContainerId, "damageContainer", "biologicalDamageContainer");
                serializer.DataField(ref resistanceSetId, "resistances", "defaultResistances");

                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                if (!prototypeManager.TryIndex(damageContainerId, out DamageContainerPrototype damageContainerData))
                {
                    throw new InvalidOperationException(
                        $"No {nameof(DamageContainerPrototype)} found with name {damageContainerId}");
                }

                CurrentDamages = new DamageContainer(damageContainerData);
                if (!prototypeManager.TryIndex(resistanceSetId, out ResistanceSetPrototype resistancesData))
                {
                    throw new InvalidOperationException(
                        $"No {nameof(ResistanceSetPrototype)} found with name {resistanceSetId}");
                }

                Resistances = new ResistanceSet(resistancesData);
            }
        }

        public bool ChangeDamage(DamageType damageType, int amount, IEntity source, bool ignoreResistances,
            HealthChangeParams extraParams = null)
        {
            if (CurrentDamages.SupportsDamageType(damageType))
            {
                var finalDamage = amount;
                if (!ignoreResistances)
                {
                    finalDamage = Resistances.CalculateDamage(damageType, amount);
                }

                var oldDamage = CurrentDamages.GetDamageValue(damageType);
                CurrentDamages.ChangeDamageValue(damageType, finalDamage);
                var data = new List<HealthChangeData>
                {
                    new HealthChangeData(damageType, CurrentDamages.GetDamageValue(damageType),
                        oldDamage - CurrentDamages.GetDamageValue(damageType))
                };

                var args = new HealthChangedEventArgs(this, data);
                OnHealthChanged(args);

                return true;
            }

            return false;
        }

        public bool ChangeDamage(DamageClass damageClass, int amount, IEntity source, bool ignoreResistances,
            HealthChangeParams extraParams = null)
        {
            if (CurrentDamages.SupportsDamageClass(damageClass))
            {
                var damageTypes = DamageContainerValues.DamageClassToType(damageClass);
                var data = new HealthChangeData[damageTypes.Count];

                if (amount < 0)
                {
                    // Changing multiple types is a bit more complicated. Might be a better way (formula?) to do this,
                    // but essentially just loops between each damage category until all healing is used up.
                    for (var i = 0; i < damageTypes.Count; i++)
                    {
                        var damage = CurrentDamages.GetDamageValue(damageTypes[i]);
                        data[i] = new HealthChangeData(damageTypes[i], damage, 0);
                    }

                    var healingLeft = amount;
                    var healThisCycle = 1;
                    // While we have healing left...
                    while (healingLeft > 0 && healThisCycle != 0)
                    {
                        // Infinite loop fallback, if no healing was done in a cycle
                        // then exit
                        healThisCycle = 0;

                        int healPerType;
                        if (healingLeft > -damageTypes.Count && healingLeft < 0)
                        {
                            // Say we were to distribute 2 healing between 3
                            // this will distribute 1 to each (and stop after 2 are given)
                            healPerType = -1;
                        }
                        else
                        {
                            // Say we were to distribute 62 healing between 3
                            // this will distribute 20 to each, leaving 2 for next loop
                            healPerType = healingLeft / damageTypes.Count;
                        }

                        for (var j = 0; j < damageTypes.Count; j++)
                        {
                            var healAmount =
                                Math.Max(Math.Max(healPerType, -CurrentDamages.GetDamageValue(damageTypes[j])),
                                    healingLeft);

                            CurrentDamages.ChangeDamageValue(damageTypes[j], healAmount);
                            healThisCycle += healAmount;
                            healingLeft -= healAmount;
                            data[j].NewValue = CurrentDamages.GetDamageValue(damageTypes[j]);
                            data[j].Delta += healAmount;
                        }
                    }

                    var heal = new HealthChangedEventArgs(this, data.ToList());
                    OnHealthChanged(heal);

                    return true;
                }

                // Similar to healing code above but simpler since we don't have to
                // account for damage being less than zero.
                for (var i = 0; i < damageTypes.Count; i++)
                {
                    data[i] = new HealthChangeData(damageTypes[i], CurrentDamages.GetDamageValue(damageTypes[i]), 0);
                }

                var damageLeft = amount;
                while (damageLeft > 0)
                {
                    int damagePerType;
                    if (damageLeft < damageTypes.Count && damageLeft > 0)
                    {
                        damagePerType = 1;
                    }
                    else
                    {
                        damagePerType = damageLeft / damageTypes.Count;
                    }

                    for (var j = 0; j < damageTypes.Count; j++)
                    {
                        var damageAmount = Math.Min(damagePerType, damageLeft);
                        CurrentDamages.ChangeDamageValue(damageTypes[j], damageAmount);
                        damageLeft -= damageAmount;
                        data[j].NewValue = CurrentDamages.GetDamageValue(damageTypes[j]);
                        data[j].Delta += damageAmount;
                    }
                }

                var args = new HealthChangedEventArgs(this, data.ToList());
                OnHealthChanged(args);

                return true;
            }

            return false;
        }

        public bool SetDamage(DamageType damageType, int newValue, IEntity source,
            HealthChangeParams extraParams = null)
        {
            if (CurrentDamages.SupportsDamageType(damageType))
            {
                var data = new HealthChangeData(damageType, newValue,
                    newValue - CurrentDamages.GetDamageValue(damageType));
                CurrentDamages.SetDamageValue(damageType, newValue);
                var dataList = new List<HealthChangeData> {data};
                var args = new HealthChangedEventArgs(this, dataList);

                OnHealthChanged(args);

                return true;
            }

            return false;
        }

        public void HealAllDamage()
        {
            var data = new List<HealthChangeData>();
            foreach (var type in CurrentDamages.SupportedDamageTypes)
            {
                data.Add(new HealthChangeData(type, 0, -CurrentDamages.GetDamageValue(type)));
                CurrentDamages.SetDamageValue(type, 0);
            }

            var args = new HealthChangedEventArgs(this, data);
            OnHealthChanged(args);
        }

        public void ForceHealthChangedEvent()
        {
            var data = new List<HealthChangeData>();
            foreach (var type in CurrentDamages.SupportedDamageTypes)
            {
                data.Add(new HealthChangeData(type, CurrentDamages.GetDamageValue(type), 0));
            }

            var args = new HealthChangedEventArgs(this, data);
            OnHealthChanged(args);
        }

        private void OnHealthChanged(HealthChangedEventArgs e)
        {
            HealthChangedEvent?.Invoke(e);
        }
    }
}
