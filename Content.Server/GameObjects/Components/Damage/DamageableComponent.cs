using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     Component that allows attached IEntities to take damage.
    ///     This basic version never dies (thus can take an indefinite amount of damage).
    /// </summary>
    [ComponentReference(typeof(BaseDamageableComponent))]
    public class DamageableComponent : BaseDamageableComponent
    {
        public override string Name => "BasicDamageable";

        [ViewVariables] private ResistanceSet Resistances { get; set; }

        [ViewVariables] private DamageContainer CurrentDamages { get; set; }

        public override List<DamageState> SupportedDamageStates => new List<DamageState> {DamageState.Alive};

        public override DamageState CurrentDamageState
        {
            get => DamageState.Alive; //This damagecontainer can tank infinite damage.
            protected set { }
        }

        public override int TotalDamage => CurrentDamages.TotalDamage;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            if (serializer.Reading)
            {
                // Doesn't write to file, TODO?
                var damageContainerID = "biologicalDamageContainer";
                var resistanceSetID = "defaultResistances";

                serializer.DataField(ref damageContainerID, "damageContainer", "biologicalDamageContainer");
                serializer.DataField(ref resistanceSetID, "resistances", "defaultResistances");

                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                if (!prototypeManager.TryIndex(damageContainerID, out DamageContainerPrototype damageContainerData))
                {
                    throw new InvalidOperationException("No DamageContainerPrototype was found with the name " +
                                                        damageContainerID + "!");
                }

                CurrentDamages = new DamageContainer(damageContainerData);
                if (!prototypeManager.TryIndex(resistanceSetID, out ResistanceSetPrototype resistancesData))
                {
                    throw new InvalidOperationException("No ResistanceSetPrototype was found with the name " +
                                                        resistanceSetID + "!");
                }

                Resistances = new ResistanceSet(resistancesData);
            }
        }

        public override bool ChangeDamage(DamageType damageType, int amount, IEntity source, bool ignoreResistances,
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
                TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
                return true;
            }

            return false;
        }

        public override bool ChangeDamage(DamageClass damageClass, int amount, IEntity source, bool ignoreResistances,
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
                    while (healingLeft > 0 && healThisCycle != 0) //While we have healing left...
                    {
                        healThisCycle = 0; //Infinite loop fallback, if no healing was done in a cycle then exit

                        int healPerType;
                        if (healingLeft > -damageTypes.Count && healingLeft < 0)
                        {
                            // Say we were to distribute 2 healing between 3, this will distribute 1 to
                            // each (and stop after 2 are given)
                            healPerType =
                                -1;
                        }
                        else
                        {
                            // Say we were to distribute 62 healing between 3, this will distribute 20 to each,
                            // leaving 2 for next loop
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

                    TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data.ToList()));
                    return true;
                }

                //Similar to healing code above but simpler since we don't have to account for damage being less than zero.
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

                TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data.ToList()));
                return true;
            }

            return false;
        }

        public override bool SetDamage(DamageType damageType, int newValue, IEntity source,
            HealthChangeParams extraParams = null)
        {
            if (CurrentDamages.SupportsDamageType(damageType))
            {
                var data = new HealthChangeData(damageType, newValue,
                    newValue - CurrentDamages.GetDamageValue(damageType));
                CurrentDamages.SetDamageValue(damageType, newValue);
                var dataList = new List<HealthChangeData> {data};
                TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, dataList));
                return true;
            }

            return false;
        }

        public override void HealAllDamage()
        {
            var data = new List<HealthChangeData>();
            foreach (var type in CurrentDamages.SupportedDamageTypes)
            {
                data.Add(new HealthChangeData(type, 0, -CurrentDamages.GetDamageValue(type)));
                CurrentDamages.SetDamageValue(type, 0);
            }

            TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
        }

        protected override void ForceHealthChangedEvent()
        {
            var data = new List<HealthChangeData>();
            foreach (var type in CurrentDamages.SupportedDamageTypes)
            {
                data.Add(new HealthChangeData(type, CurrentDamages.GetDamageValue(type), 0));
            }

            TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
        }
    }
}
