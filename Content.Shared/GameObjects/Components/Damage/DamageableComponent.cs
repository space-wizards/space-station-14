#nullable enable
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

namespace Content.Shared.GameObjects.Components.Damage
{
    /// <summary>
    ///     Component that allows attached entities to take damage.
    ///     This basic version never dies (thus can take an indefinite amount of damage).
    /// </summary>
    [ComponentReference(typeof(IDamageableComponent))]
    public class DamageableComponent : Component, IDamageableComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
#pragma warning restore 649

        public override string Name => "BasicDamageable";

        public event Action<HealthChangedEventArgs> HealthChangedEvent = default!;

        [ViewVariables] private ResistanceSet Resistance { get; set; } = default!;

        [ViewVariables] private DamageContainer Damage { get; set; } = default!;

        public virtual List<DamageState> SupportedDamageStates => new List<DamageState> {DamageState.Alive};

        [ViewVariables] public virtual DamageState CurrentDamageState { get; protected set; } = DamageState.Alive;

        public int TotalDamage => Damage.TotalDamage;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                // Doesn't write to file, TODO?
                // Yes, TODO
                var containerId = "biologicalDamageContainer";
                var resistanceId = "defaultResistances";

                serializer.DataField(ref containerId, "damageContainer", "biologicalDamageContainer");
                serializer.DataField(ref resistanceId, "resistances", "defaultResistances");

                if (!_prototypeManager.TryIndex(containerId!, out DamageContainerPrototype damage))
                {
                    throw new InvalidOperationException(
                        $"No {nameof(DamageContainerPrototype)} found with name {containerId}");
                }

                Damage = new DamageContainer(damage);

                if (!_prototypeManager.TryIndex(resistanceId!, out ResistanceSetPrototype resistance))
                {
                    throw new InvalidOperationException(
                        $"No {nameof(ResistanceSetPrototype)} found with name {resistanceId}");
                }

                Resistance = new ResistanceSet(resistance);
            }
        }

        public bool ChangeDamage(DamageType type, int amount, bool ignoreResistances,
            IEntity? source,
            HealthChangeParams? extraParams = null)
        {
            if (Damage.SupportsDamageType(type))
            {
                var finalDamage = amount;
                if (!ignoreResistances)
                {
                    finalDamage = Resistance.CalculateDamage(type, amount);
                }

                Damage.ChangeDamageValue(type, finalDamage);

                var oldDamage = Damage.GetDamageValue(type);
                var damage = Damage.GetDamageValue(type);
                var delta = oldDamage - Damage.GetDamageValue(type);
                var datum = new HealthChangeData(type, damage, delta);
                var data = new List<HealthChangeData> {datum};
                var args = new HealthChangedEventArgs(this, data);

                OnHealthChanged(args);

                return true;
            }

            return false;
        }

        public bool ChangeDamage(DamageClass @class, int amount, bool ignoreResistances,
            IEntity? source,
            HealthChangeParams? extraParams = null)
        {
            if (Damage.SupportsDamageClass(@class))
            {
                var damageTypes = @class.ToType();
                var data = new HealthChangeData[damageTypes.Count];

                if (amount < 0)
                {
                    // Changing multiple types is a bit more complicated. Might be a better way (formula?) to do this,
                    // but essentially just loops between each damage category until all healing is used up.
                    for (var i = 0; i < damageTypes.Count; i++)
                    {
                        var damage = Damage.GetDamageValue(damageTypes[i]);
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
                                Math.Max(Math.Max(healPerType, -Damage.GetDamageValue(damageTypes[j])),
                                    healingLeft);

                            Damage.ChangeDamageValue(damageTypes[j], healAmount);
                            healThisCycle += healAmount;
                            healingLeft -= healAmount;

                            data[j].NewValue = Damage.GetDamageValue(damageTypes[j]);
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
                    data[i] = new HealthChangeData(damageTypes[i], Damage.GetDamageValue(damageTypes[i]), 0);
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
                        Damage.ChangeDamageValue(damageTypes[j], damageAmount);
                        damageLeft -= damageAmount;

                        data[j].NewValue = Damage.GetDamageValue(damageTypes[j]);
                        data[j].Delta += damageAmount;
                    }
                }

                var args = new HealthChangedEventArgs(this, data.ToList());
                OnHealthChanged(args);

                return true;
            }

            return false;
        }

        public bool SetDamage(DamageType type, int newValue, IEntity source,
            HealthChangeParams? extraParams = null)
        {
            if (Damage.SupportsDamageType(type))
            {
                var delta = newValue - Damage.GetDamageValue(type);
                var data = new HealthChangeData(type, newValue, delta);
                Damage.SetDamageValue(type, newValue);
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

            foreach (var type in Damage.SupportedDamageTypes)
            {
                var delta = -Damage.GetDamageValue(type);
                var datum = new HealthChangeData(type, 0, delta);
                data.Add(datum);
                Damage.SetDamageValue(type, 0);
            }

            var args = new HealthChangedEventArgs(this, data);

            OnHealthChanged(args);
        }

        public void ForceHealthChangedEvent()
        {
            var data = new List<HealthChangeData>();

            foreach (var type in Damage.SupportedDamageTypes)
            {
                var damage = Damage.GetDamageValue(type);
                var datum = new HealthChangeData(type, damage, 0);
                data.Add(datum);
            }

            var args = new HealthChangedEventArgs(this, data);

            OnHealthChanged(args);
        }

        protected virtual void OnHealthChanged(HealthChangedEventArgs e)
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, e);
            HealthChangedEvent?.Invoke(e);
            Dirty();
        }
    }
}
