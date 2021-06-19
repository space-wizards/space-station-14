#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Acts;
using Content.Shared.Damage.Container;
using Content.Shared.Damage.Resistances;
using Content.Shared.NetIDs;
using Content.Shared.Radiation;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.Components
{
    /// <summary>
    ///     Component that allows attached entities to take damage.
    ///     This basic version never dies (thus can take an indefinite amount of damage).
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class DamageableComponent : Component, IDamageableComponent, IRadiationAct, ISerializationHooks
    {
        public override string Name => "Damageable";

        public override uint? NetID => ContentNetIDs.DAMAGEABLE;

        // TODO define these in yaml?
        public const string DefaultResistanceSet = "defaultResistances";
        public const string DefaultDamageContainer = "metallicDamageContainer";

        private readonly Dictionary<DamageType, int> _damageList = DamageTypeExtensions.ToNewDictionary();

        [DataField("resistances")] public string ResistanceSetId = DefaultResistanceSet;

        // TODO DAMAGE Use as default values, specify overrides in a separate property through yaml for better (de)serialization
        [ViewVariables] [DataField("damageContainer")] public string DamageContainerId { get; set; } = DefaultDamageContainer;

        [ViewVariables] public ResistanceSet Resistances { get; set; } = new();

        // TODO DAMAGE Cache this
        [ViewVariables] public int TotalDamage => _damageList.Values.Sum();

        [ViewVariables] public IReadOnlyDictionary<DamageClass, int> DamageClasses => _damageList.ToClassDictionary();

        [ViewVariables] public IReadOnlyDictionary<DamageType, int> DamageTypes => _damageList;

        [ViewVariables] public HashSet<DamageType> SupportedTypes { get; } = new();

        [ViewVariables] public HashSet<DamageClass> SupportedClasses { get; } = new();

        public bool SupportsDamageClass(DamageClass @class)
        {
            return SupportedClasses.Contains(@class);
        }

        public bool SupportsDamageType(DamageType type)
        {
            return SupportedTypes.Contains(type);
        }

        public override void Initialize()
        {
            base.Initialize();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            // TODO DAMAGE Serialize damage done and resistance changes
            var damagePrototype = prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);

            SupportedClasses.Clear();
            SupportedTypes.Clear();

            DamageContainerId = damagePrototype.ID;
            SupportedClasses.UnionWith(damagePrototype.SupportedClasses);
            SupportedTypes.UnionWith(damagePrototype.SupportedTypes);

            var resistancePrototype = prototypeManager.Index<ResistanceSetPrototype>(ResistanceSetId);
            Resistances = new ResistanceSet(resistancePrototype);
        }

        protected override void Startup()
        {
            base.Startup();

            ForceHealthChangedEvent();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new DamageableComponentState(_damageList);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is DamageableComponentState state))
            {
                return;
            }

            _damageList.Clear();

            foreach (var (type, damage) in state.DamageList)
            {
                _damageList[type] = damage;
            }
        }

        public int GetDamage(DamageType type)
        {
            return _damageList.GetValueOrDefault(type);
        }

        public bool TryGetDamage(DamageType type, out int damage)
        {
            return _damageList.TryGetValue(type, out damage);
        }

        public int GetDamage(DamageClass @class)
        {
            if (!SupportsDamageClass(@class))
            {
                return 0;
            }

            var damage = 0;

            foreach (var type in @class.ToTypes())
            {
                damage += GetDamage(type);
            }

            return damage;
        }

        public bool TryGetDamage(DamageClass @class, out int damage)
        {
            if (!SupportsDamageClass(@class))
            {
                damage = 0;
                return false;
            }

            damage = GetDamage(@class);
            return true;
        }

        /// <summary>
        ///     Attempts to set the damage value for the given <see cref="DamageType"/>.
        /// </summary>
        /// <returns>
        ///     True if successful, false if this container does not support that type.
        /// </returns>
        public bool TrySetDamage(DamageType type, int newValue)
        {
            if (newValue < 0)
            {
                return false;
            }

            var damageClass = type.ToClass();

            if (SupportedClasses.Contains(damageClass))
            {
                var old = _damageList[type] = newValue;
                _damageList[type] = newValue;

                var delta = newValue - old;
                var datum = new DamageChangeData(type, newValue, delta);
                var data = new List<DamageChangeData> {datum};

                OnHealthChanged(data);

                return true;
            }

            return false;
        }

        public void Heal(DamageType type)
        {
            SetDamage(type, 0);
        }

        public void Heal()
        {
            foreach (var type in SupportedTypes)
            {
                Heal(type);
            }
        }

        public bool ChangeDamage(
            DamageType type,
            int amount,
            bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            if (!SupportsDamageType(type))
            {
                return false;
            }

            var finalDamage = amount;

            if (!ignoreResistances)
            {
                finalDamage = Resistances.CalculateDamage(type, amount);
            }

            if (finalDamage == 0)
                return false;

            if (!_damageList.TryGetValue(type, out var current))
            {
                return false;
            }

            if (current + finalDamage < 0)
            {
                if (current == 0)
                    return false;
                _damageList[type] = 0;
                finalDamage = -current;
            }
            else
            {
                _damageList[type] = current + finalDamage;
            }

            current = _damageList[type];

            var datum = new DamageChangeData(type, current, finalDamage);
            var data = new List<DamageChangeData> {datum};

            OnHealthChanged(data);

            return true;
        }

        public bool ChangeDamage(DamageClass @class, int amount, bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            if (!SupportsDamageClass(@class))
            {
                return false;
            }

            var types = @class.ToTypes();

            if (amount < 0)
            {
                // Changing multiple types is a bit more complicated. Might be a better way (formula?) to do this,
                // but essentially just loops between each damage category until all healing is used up.
                var healingLeft = -amount;
                var healThisCycle = 1;

                // While we have healing left...
                while (healingLeft > 0 && healThisCycle != 0)
                {
                    // Infinite loop fallback, if no healing was done in a cycle
                    // then exit
                    healThisCycle = 0;

                    int healPerType;
                    if (healingLeft < types.Count)
                    {
                        // Say we were to distribute 2 healing between 3
                        // this will distribute 1 to each (and stop after 2 are given)
                        healPerType = 1;
                    }
                    else
                    {
                        // Say we were to distribute 62 healing between 3
                        // this will distribute 20 to each, leaving 2 for next loop
                        healPerType = healingLeft / types.Count;
                    }

                    foreach (var type in types)
                    {
                        var damage = GetDamage(type);
                        var healAmount = Math.Min(healingLeft, damage);
                        healAmount = Math.Min(healAmount, healPerType);

                        ChangeDamage(type, -healAmount, true);
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

                if (damageLeft < types.Count)
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
                    ChangeDamage(type, damageAmount, true);
                    damageLeft -= damageAmount;
                }
            }

            return true;
        }

        public bool SetDamage(DamageType type, int newValue, IEntity? source = null,  DamageChangeParams? extraParams = null)
        {
            if (newValue >= TotalDamage)
            {
                return false;
            }

            if (newValue < 0)
            {
                return false;
            }

            if (!_damageList.ContainsKey(type))
            {
                return false;
            }

            var old = _damageList[type];
            _damageList[type] = newValue;

            var delta = newValue - old;
            var datum = new DamageChangeData(type, 0, delta);
            var data = new List<DamageChangeData> {datum};

            OnHealthChanged(data);

            return true;
        }

        public void ForceHealthChangedEvent()
        {
            var data = new List<DamageChangeData>();

            foreach (var type in SupportedTypes)
            {
                var damage = GetDamage(type);
                var datum = new DamageChangeData(type, damage, 0);
                data.Add(datum);
            }

            OnHealthChanged(data);
        }

        private void OnHealthChanged(List<DamageChangeData> changes)
        {
            var args = new DamageChangedEventArgs(this, changes);
            OnHealthChanged(args);
        }

        protected virtual void OnHealthChanged(DamageChangedEventArgs e)
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, e);

            var message = new DamageChangedMessage(this, e.Data);
            SendMessage(message);

            Dirty();
        }

        void IRadiationAct.RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
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

    [Serializable, NetSerializable]
    public class DamageableComponentState : ComponentState
    {
        public readonly Dictionary<DamageType, int> DamageList;

        public DamageableComponentState(Dictionary<DamageType, int> damageList) : base(ContentNetIDs.DAMAGEABLE)
        {
            DamageList = damageList;
        }
    }
}
