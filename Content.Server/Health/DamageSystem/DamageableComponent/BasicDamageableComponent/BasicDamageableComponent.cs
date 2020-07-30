 using Content.Shared.DamageSystem;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.DamageSystem
{

    /// <summary>
    ///     Component that allows attached IEntities to take damage. This basic version never dies (thus can take an indefinite amount of damage).
    /// </summary>
    [ComponentReference(typeof(IDamageableComponent))]
    public class BasicDamageableComponent : IDamageableComponent
    {
        public override string Name => "BasicDamageable";

        [ViewVariables]
        public ResistanceSet Resistances { get; private set; }

        [ViewVariables]
        public DamageContainer CurrentDamages { get; private set; }

        public override List<DamageState> SupportedDamageStates => new List<DamageState> { DamageState.Alive };

        public override DamageState CurrentDamageState
        {
            get
            {
                return DamageState.Alive; //This damagecontainer can tank infinite damage.
            }
            protected set
            {
                return;
            }
        }

        public override int TotalDamage
        {
            get
            {
                return CurrentDamages.TotalDamage;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            if (serializer.Reading){ //Doesn't write to file, TODO?
                string damageContainerID = "biologicalDamageContainer";
                string resistanceSetID = "defaultResistances";

                serializer.DataField(ref damageContainerID, "damageContainer", "biologicalDamageContainer");
                serializer.DataField(ref resistanceSetID, "resistances", "defaultResistances");

                IPrototypeManager prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                if (!prototypeManager.TryIndex(damageContainerID, out DamageContainerPrototype damageContainerData))
                    throw new InvalidOperationException("No DamageContainerPrototype was found with the name " + damageContainerID + "!");
                CurrentDamages = new DamageContainer(damageContainerData);
                if (!prototypeManager.TryIndex(resistanceSetID, out ResistanceSetPrototype resistancesData))
                    throw new InvalidOperationException("No ResistanceSetPrototype was found with the name " + resistanceSetID + "!");
                Resistances = new ResistanceSet(resistancesData);
            }

        }

        public override bool ChangeDamage(DamageType damageType, int amount, IEntity source, bool ignoreResistances, HealthChangeParams extraParams = null)
        {
            if (CurrentDamages.SupportsDamageType(damageType)){
                int finalDamage = amount;
                if(!ignoreResistances)
                    finalDamage = Resistances.CalculateDamage(damageType, amount);
                int oldDamage = CurrentDamages.GetDamageValue(damageType);
                CurrentDamages.ChangeDamageValue(damageType, finalDamage);
                List<HealthChangeData> data = new List<HealthChangeData> { new HealthChangeData(damageType, CurrentDamages.GetDamageValue(damageType), oldDamage-CurrentDamages.GetDamageValue(damageType)) };
                TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
                return true;
            }
            return false;
        }

        public override bool ChangeDamage(DamageClass damageClass, int amount, IEntity source, bool ignoreResistances, HealthChangeParams extraParams = null)
        {
            if (CurrentDamages.SupportsDamageClass(damageClass))
            {
                List<DamageType> damageTypes = DamageContainerValues.DamageClassToType(damageClass);
                HealthChangeData[] data = new HealthChangeData[damageTypes.Count];
                if (amount < 0) { //Changing multiple types is a bit more complicated. Might be a better way (formula?) to do this, but essentially just loops between each damage category until all healing is used up.
                    for(int i = 0;i < damageTypes.Count;i++){
                        data[i] = new HealthChangeData(damageTypes[i], CurrentDamages.GetDamageValue(damageTypes[i]), 0);
                    }
                    int healingLeft = amount;
                    int healThisCycle = 1;
                    int healPerType;
                    while (healingLeft > 0 && healThisCycle != 0) //While we have healing left...
                    {
                        healThisCycle = 0; //Infinite loop fallback, if no healing was done in a cycle then exit
                        
                        if (healingLeft > -damageTypes.Count && healingLeft < 0)
                            healPerType = -1; //Say we were to distribute 2 healing between 3, this will distribute 1 to each (and stop after 2 are given)
                        else
                            healPerType = healingLeft / damageTypes.Count; //Say we were to distribute 62 healing between 3, this will distribute 20 to each, leaving 2 for next loop
                        for (int j = 0; j < damageTypes.Count; j++)
                        {
                            int healAmount = Math.Max(Math.Max(healPerType, -CurrentDamages.GetDamageValue(damageTypes[j])), healingLeft);
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
                else { //Similar to healing code above but simpler since we don't have to account for damage being less than zero.
                    for (int i = 0; i < damageTypes.Count; i++)
                    {
                        data[i] = new HealthChangeData(damageTypes[i], CurrentDamages.GetDamageValue(damageTypes[i]), 0);
                    }
                    int damageLeft = amount;
                    int damagePerType;
                    while (damageLeft > 0) 
                    {
                        if (damageLeft < damageTypes.Count && damageLeft > 0)
                            damagePerType = 1; 
                        else
                            damagePerType = damageLeft / damageTypes.Count; 
                        for (int j = 0; j < damageTypes.Count; j++)
                        {
                            int damageAmount = Math.Min(damagePerType, damageLeft);
                            CurrentDamages.ChangeDamageValue(damageTypes[j], damageAmount);
                            damageLeft -= damageAmount;
                            data[j].NewValue = CurrentDamages.GetDamageValue(damageTypes[j]);
                            data[j].Delta += damageAmount;
                        }
                    }
                    TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data.ToList()));
                    return true;
                }
            }
            return false;
        }

        public override bool SetDamage(DamageType damageType, int newValue, IEntity source, HealthChangeParams extraParams = null)
        {
            if (CurrentDamages.SupportsDamageType(damageType))
            {
                HealthChangeData data = new HealthChangeData(damageType, newValue, newValue-CurrentDamages.GetDamageValue(damageType));
                CurrentDamages.SetDamageValue(damageType, newValue);
                List<HealthChangeData> dataList = new List<HealthChangeData> { data };
                TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, dataList));
                return true;
            }
            return false;
        }

        public override void HealAllDamage()
        {
            List<HealthChangeData> data = new List<HealthChangeData>();
            foreach (DamageType type in CurrentDamages.SupportedDamageTypes)
            {
                data.Add(new HealthChangeData(type, 0, -CurrentDamages.GetDamageValue(type)));
                CurrentDamages.SetDamageValue(type, 0);
            }
            TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
        }

        public override void ForceHealthChangedEvent() {
            List<HealthChangeData> data = new List<HealthChangeData>();
            foreach (DamageType type in CurrentDamages.SupportedDamageTypes)
            {
                data.Add(new HealthChangeData(type, CurrentDamages.GetDamageValue(type), 0));
            }
            TryInvokeHealthChangedEvent(new HealthChangedEventArgs(this, data));
        }
    }
}
