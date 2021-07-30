using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Acts;
using Content.Shared.Damage.Container;
using Content.Shared.Damage.Resistances;
using Content.Shared.Radiation;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
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
    [NetworkedComponent()]
    public class DamageableComponent : Component, IDamageableComponent, IRadiationAct, ISerializationHooks
    {
        public override string Name => "Damageable";

        private IPrototypeManager _prototypeManager = default!;
        private Dictionary<DamageTypePrototype, int> _damageList = new();

        // TODO define these in yaml?
        public const string DefaultResistanceSet = "defaultResistances";
        public const string DefaultDamageContainer = "metallicDamageContainer";

        [DataField("resistances")]
        public string ResistanceSetId { get; set; } = DefaultResistanceSet;

        [ViewVariables] public ResistanceSet Resistances { get; set; } = new();

        // TODO DAMAGE Use as default values, specify overrides in a separate property through yaml for better (de)serialization
        [ViewVariables]
        [DataField("damageContainer")]
        public string DamageContainerId { get; set; } = DefaultDamageContainer;

        // TODO DAMAGE Cache this
        [ViewVariables] public int TotalDamage => _damageList.Values.Sum();
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> DamageClasses => DamageListToDamageGroup(_damageList);
        [ViewVariables] public IReadOnlyDictionary<DamageTypePrototype, int> DamageTypes => _damageList;

        public HashSet<DamageGroupPrototype> SupportedGroups { get; } = new();

        public HashSet<DamageTypePrototype> SupportedTypes { get; } = new();

        protected override void Initialize()
        {
            base.Initialize();
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            // TODO DAMAGE Serialize damage done and resistance changes
            var damageContainerPrototype = _prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);

            SupportedGroups.Clear();
            SupportedTypes.Clear();

            DamageContainerId = damageContainerPrototype.ID;
            SupportedGroups.UnionWith(damageContainerPrototype.SupportedDamageGroups);
            SupportedTypes.UnionWith(damageContainerPrototype.SupportedDamageTypes);

            foreach (var DamageType in SupportedTypes)
            {
                _damageList.Add(DamageType,0);
            }

            var resistancePrototype = _prototypeManager.Index<ResistanceSetPrototype>(ResistanceSetId);
            Resistances = new ResistanceSet(resistancePrototype);
        }

        public bool SupportsDamageClass(DamageGroupPrototype group)
        {
            return SupportedGroups.Contains(group);
        }

        public bool SupportsDamageType(DamageTypePrototype type)
        {
            return SupportedTypes.Contains(type);
        }

        protected override void Startup()
        {
            base.Startup();

            ForceHealthChangedEvent();
        }

        public DamageTypePrototype GetDamageType(string ID)
        {
            return _prototypeManager.Index<DamageTypePrototype>(ID);
        }

        public DamageGroupPrototype GetDamageGroup(string ID)
        {
            return _prototypeManager.Index<DamageGroupPrototype>(ID);
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

        public int GetDamage(DamageTypePrototype type)
        {
            return _damageList.GetValueOrDefault(type);
        }

        public bool TryGetDamage(DamageTypePrototype type, out int damage)
        {
            return _damageList.TryGetValue(type, out damage);
        }

        public int GetDamage(DamageGroupPrototype group)
        {
            if (!SupportsDamageClass(group))
            {
                return 0;
            }

            var damage = 0;

            foreach (var type in group.DamageTypes)
            {
                damage += GetDamage(type);
            }

            return damage;
        }

        public bool TryGetDamage(DamageGroupPrototype group, out int damage)
        {
            if (!SupportsDamageClass(group))
            {
                damage = 0;
                return false;
            }

            damage = GetDamage(group);
            return true;
        }

        /// <summary>
        ///     Attempts to set the damage value for the given <see cref="DamageTypePrototype"/>.
        /// </summary>
        /// <returns>
        ///     True if successful, false if this container does not support that type.
        /// </returns>
        public bool TrySetDamage(DamageTypePrototype type, int newValue)
        {
            if (newValue < 0)
            {
                return false;
            }

            if (SupportedTypes.Contains(type))
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

        public void Heal(DamageTypePrototype type)
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
            DamageTypePrototype type,
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

        public bool ChangeDamage(DamageGroupPrototype group, int amount, bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            if (!SupportsDamageClass(group))
            {
                return false;
            }

            var types = group.DamageTypes.ToArray();

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
                    if (healingLeft < types.Length)
                    {
                        // Say we were to distribute 2 healing between 3
                        // this will distribute 1 to each (and stop after 2 are given)
                        healPerType = 1;
                    }
                    else
                    {
                        // Say we were to distribute 62 healing between 3
                        // this will distribute 20 to each, leaving 2 for next loop
                        healPerType = healingLeft / types.Length;
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

                if (damageLeft < types.Length)
                {
                    damagePerType = 1;
                }
                else
                {
                    damagePerType = damageLeft / types.Length;
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

        public bool SetDamage(DamageTypePrototype type, int newValue, IEntity? source = null,  DamageChangeParams? extraParams = null)
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

        private IReadOnlyDictionary<DamageGroupPrototype, int> DamageListToDamageGroup(IReadOnlyDictionary<DamageTypePrototype, int> damagelist)
        {
            var damageGroupDict = new Dictionary<DamageGroupPrototype, int>();
            int damageGroupSumDamage = 0;
            int damageTypeDamage = 0 ;
            foreach (var damageGroup in SupportedGroups)
            {
                damageGroupSumDamage = 0;
                foreach (var damageType in SupportedTypes)
                {
                    damageTypeDamage = 0;
                     damagelist.TryGetValue(damageType,out damageTypeDamage);
                     damageGroupSumDamage += damageTypeDamage;
                }
                damageGroupDict.Add(damageGroup,damageGroupSumDamage);
            }

            return damageGroupDict;
        }

        protected virtual void OnHealthChanged(DamageChangedEventArgs e)
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, e);

            var message = new DamageChangedMessage(this, e.Data);
            SendMessage(message);

            Dirty();
        }

        public void RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            var totalDamage = Math.Max((int)(frameTime * radiation.RadsPerSecond), 1);

            ChangeDamage(GetDamageType("Radiation"), totalDamage, false, radiation.Owner);
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

            ChangeDamage(GetDamageType("Piercing"), damage, false);
            ChangeDamage(GetDamageType("Heat"), damage, false);
        }
    }

    [Serializable, NetSerializable]
    public class DamageableComponentState : ComponentState
    {
        public readonly Dictionary<DamageTypePrototype, int> DamageList;

        public DamageableComponentState(Dictionary<DamageTypePrototype, int> damageList) 

        {
            DamageList = damageList;
        }
    }
}
