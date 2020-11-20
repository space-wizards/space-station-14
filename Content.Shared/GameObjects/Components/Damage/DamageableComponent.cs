#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
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
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class DamageableComponent : Component, IDamageableComponent, IRadiationAct
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        // TODO define these in yaml?
        public const string DefaultDamageContainer = "metallicDamageContainer";
        public const string DefaultResistanceSet = "defaultResistances";

        public override string Name => "Damageable";

        private DamageState _damageState;
        private DamageFlag _flags;

        public event Action<HealthChangedEventArgs>? HealthChangedEvent;

        [ViewVariables] private ResistanceSet Resistances { get; set; } = default!;

        [ViewVariables] private DamageContainer Damage { get; set; } = default!;

        public Dictionary<DamageState, int> Thresholds { get; set; } = new();

        public virtual List<DamageState> SupportedDamageStates
        {
            get
            {
                var states = new List<DamageState> {DamageState.Alive};

                states.AddRange(Thresholds.Keys);

                return states;
            }
        }

        public virtual DamageState CurrentState
        {
            get => _damageState;
            set
            {
                var old = _damageState;
                _damageState = value;

                if (old != value)
                {
                    EnterState(value);
                }

                Dirty();
            }
        }

        [ViewVariables] public int TotalDamage => Damage.TotalDamage;

        public IReadOnlyDictionary<DamageClass, int> DamageClasses => Damage.DamageClasses;

        public IReadOnlyDictionary<DamageType, int> DamageTypes => Damage.DamageTypes;

        public DamageFlag Flags
        {
            get => _flags;
            private set
            {
                if (_flags == value)
                {
                    return;
                }

                _flags = value;
                Dirty();
            }
        }

        public void AddFlag(DamageFlag flag)
        {
            Flags |= flag;
        }

        public bool HasFlag(DamageFlag flag)
        {
            return Flags.HasFlag(flag);
        }

        public void RemoveFlag(DamageFlag flag)
        {
            Flags &= ~flag;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO DAMAGE Serialize as a dictionary of damage states to thresholds
            serializer.DataReadWriteFunction(
                "criticalThreshold",
                null,
                t =>
                {
                    if (t == null)
                    {
                        return;
                    }

                    Thresholds[DamageState.Critical] = t.Value;
                },
                () => Thresholds.TryGetValue(DamageState.Critical, out var value) ? value : (int?) null);

            serializer.DataReadWriteFunction(
                "deadThreshold",
                null,
                t =>
                {
                    if (t == null)
                    {
                        return;
                    }

                    Thresholds[DamageState.Dead] = t.Value;
                },
                () => Thresholds.TryGetValue(DamageState.Dead, out var value) ? value : (int?) null);

            serializer.DataField(ref _damageState, "damageState", DamageState.Alive);

            serializer.DataReadWriteFunction(
                "flags",
                new List<DamageFlag>(),
                flags =>
                {
                    var result = DamageFlag.None;

                    foreach (var flag in flags)
                    {
                        result |= flag;
                    }

                    Flags = result;
                },
                () =>
                {
                    var writeFlags = new List<DamageFlag>();

                    if (Flags == DamageFlag.None)
                        return writeFlags;

                    foreach (var flag in (DamageFlag[]) Enum.GetValues(typeof(DamageFlag)))
                    {
                        if ((Flags & flag) == flag)
                        {
                            writeFlags.Add(flag);
                        }
                    }

                    return writeFlags;
                });

            // TODO DAMAGE Serialize damage done and resistance changes
            serializer.DataReadWriteFunction(
                "damagePrototype",
                DefaultDamageContainer,
                prototype =>
                {
                    var damagePrototype = _prototypeManager.Index<DamageContainerPrototype>(prototype);
                    Damage = new DamageContainer(OnHealthChanged, damagePrototype);
                },
                () => Damage.ID);

            serializer.DataReadWriteFunction(
                "resistancePrototype",
                DefaultResistanceSet,
                prototype =>
                {
                    var resistancePrototype = _prototypeManager.Index<ResistanceSetPrototype>(prototype);
                    Resistances = new ResistanceSet(resistancePrototype);
                },
                () => Resistances.ID);
        }

        public override void Initialize()
        {
            base.Initialize();

            foreach (var behavior in Owner.GetAllComponents<IOnHealthChangedBehavior>())
            {
                HealthChangedEvent += behavior.OnHealthChanged;
            }
        }

        protected override void Startup()
        {
            base.Startup();

            ForceHealthChangedEvent();
        }

        public bool TryGetDamage(DamageType type, out int damage)
        {
            return Damage.TryGetDamageValue(type, out damage);
        }

        public bool ChangeDamage(DamageType type, int amount, bool ignoreResistances,
            IEntity? source = null,
            HealthChangeParams? extraParams = null)
        {
            if (amount > 0 && HasFlag(DamageFlag.Invulnerable))
            {
                return false;
            }

            if (Damage.SupportsDamageType(type))
            {
                var finalDamage = amount;
                if (!ignoreResistances)
                {
                    finalDamage = Resistances.CalculateDamage(type, amount);
                }

                Damage.ChangeDamageValue(type, finalDamage);

                return true;
            }

            return false;
        }

        public bool ChangeDamage(DamageClass @class, int amount, bool ignoreResistances,
            IEntity? source = null,
            HealthChangeParams? extraParams = null)
        {
            if (amount > 0 && HasFlag(DamageFlag.Invulnerable))
            {
                return false;
            }

            if (Damage.SupportsDamageClass(@class))
            {
                var types = @class.ToTypes();

                if (amount < 0)
                {
                    // Changing multiple types is a bit more complicated. Might be a better way (formula?) to do this,
                    // but essentially just loops between each damage category until all healing is used up.
                    var healingLeft = amount;
                    var healThisCycle = 1;

                    // While we have healing left...
                    while (healingLeft > 0 && healThisCycle != 0)
                    {
                        // Infinite loop fallback, if no healing was done in a cycle
                        // then exit
                        healThisCycle = 0;

                        int healPerType;
                        if (healingLeft > -types.Count && healingLeft < 0)
                        {
                            // Say we were to distribute 2 healing between 3
                            // this will distribute 1 to each (and stop after 2 are given)
                            healPerType = -1;
                        }
                        else
                        {
                            // Say we were to distribute 62 healing between 3
                            // this will distribute 20 to each, leaving 2 for next loop
                            healPerType = healingLeft / types.Count;
                        }

                        foreach (var type in types)
                        {
                            var healAmount =
                                Math.Max(Math.Max(healPerType, -Damage.GetDamageValue(type)),
                                    healingLeft);

                            Damage.ChangeDamageValue(type, healAmount);
                            healThisCycle += healAmount;
                            healingLeft -= healAmount;
                        }
                    }

                    return true;
                }

                var damageLeft = amount;

                while (damageLeft > 0)
                {
                    int damagePerType;

                    if (damageLeft < types.Count && damageLeft > 0)
                    {
                        damagePerType = 1;
                    }
                    else
                    {
                        damagePerType = damageLeft / types.Count;
                    }

                    foreach (var type in types)
                    {
                        var damageAmount = Math.Min(damagePerType, damageLeft);
                        Damage.ChangeDamageValue(type, damageAmount);
                        damageLeft -= damageAmount;
                    }
                }

                return true;
            }

            return false;
        }

        public bool SetDamage(DamageType type, int newValue, IEntity? source = null,
            HealthChangeParams? extraParams = null)
        {
            if (newValue >= TotalDamage && HasFlag(DamageFlag.Invulnerable))
            {
                return false;
            }

            if (Damage.SupportsDamageType(type))
            {
                Damage.SetDamageValue(type, newValue);

                return true;
            }

            return false;
        }

        public void Heal()
        {
            Damage.Heal();
        }

        public void ForceHealthChangedEvent()
        {
            var data = new List<HealthChangeData>();

            foreach (var type in Damage.SupportedTypes)
            {
                var damage = Damage.GetDamageValue(type);
                var datum = new HealthChangeData(type, damage, 0);
                data.Add(datum);
            }

            OnHealthChanged(data);
        }

        public (int current, int max)? Health(DamageState threshold)
        {
            if (!SupportedDamageStates.Contains(threshold) ||
                !Thresholds.TryGetValue(threshold, out var thresholdValue))
            {
                return null;
            }

            var current = thresholdValue - TotalDamage;
            return (current, thresholdValue);
        }

        public bool TryHealth(DamageState threshold, out (int current, int max) health)
        {
            var temp = Health(threshold);

            if (temp == null)
            {
                health = (default, default);
                return false;
            }

            health = temp.Value;
            return true;
        }

        private void OnHealthChanged(List<HealthChangeData> changes)
        {
            var args = new HealthChangedEventArgs(this, changes);
            OnHealthChanged(args);
        }

        protected virtual void EnterState(DamageState state) { }

        protected virtual void OnHealthChanged(HealthChangedEventArgs e)
        {
            if (CurrentState != DamageState.Dead)
            {
                if (Thresholds.TryGetValue(DamageState.Dead, out var deadThreshold) &&
                    TotalDamage > deadThreshold)
                {
                    CurrentState = DamageState.Dead;
                }
                else if (Thresholds.TryGetValue(DamageState.Critical, out var critThreshold) &&
                         TotalDamage > critThreshold)
                {
                    CurrentState = DamageState.Critical;
                }
                else
                {
                    CurrentState = DamageState.Alive;
                }
            }

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, e);
            HealthChangedEvent?.Invoke(e);

            Dirty();
        }

        public void RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            var totalDamage = Math.Max((int)(frameTime * radiation.RadsPerSecond), 1);

            ChangeDamage(DamageType.Radiation, totalDamage, false, radiation.Owner);
        }

        public void OnExplosion(ExplosionEventArgs eventArgs)
        {
            var damage = eventArgs.Severity switch
            {
                ExplosionSeverity.Light => 20,
                ExplosionSeverity.Heavy => 60,
                ExplosionSeverity.Destruction => 250,
                _ => throw new ArgumentOutOfRangeException()
            };

            ChangeDamage(DamageType.Piercing, damage, false);
            ChangeDamage(DamageType.Heat, damage, false);
        }
    }
}
