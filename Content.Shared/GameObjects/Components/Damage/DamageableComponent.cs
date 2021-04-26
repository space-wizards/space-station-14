#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Damage
{
    /// <summary>
    ///     Component that allows attached entities to take damage.
    ///     This basic version never dies (thus can take an indefinite amount of damage).
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class DamageableComponent : Component, IDamageableComponent, IRadiationAct, ISerializationHooks
    {
        private Dictionary<string, DamageTypePrototype> _damageTypeDict= default!;
        private readonly Dictionary<DamageTypePrototype, int> _damageList = default!;
        private readonly HashSet<DamageTypePrototype> _supportedDamageTypes = new();
        private readonly HashSet<DamageGroupPrototype> _supportedDamageGroups = new();
        [ViewVariables] private ResistanceSet Resistances { get; set; } = new();

        public override string Name => "Damageable";

        public override uint? NetID => ContentNetIDs.DAMAGEABLE;

        public bool Godmode { get; set; }

        // TODO define these in yaml?
        public const string DefaultResistanceSet = "defaultResistances";
        public const string DefaultDamageContainer = "metallicDamageContainer";

        [DataField("resistances")]
        public string ResistanceSetId = DefaultResistanceSet;



        // TODO DAMAGE Use as default values, specify overrides in a separate property through yaml for better (de)serialization
        [ViewVariables] [DataField("damageContainer")]
        public string DamageContainerId { get; set; } = DefaultDamageContainer;

        // TODO DAMAGE Cache this
        [ViewVariables] public int TotalDamage => _damageList.Values.Sum();
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> DamageClasses => damageListToDamageGroup(_damageList);
        [ViewVariables] public IReadOnlyDictionary<DamageTypePrototype, int> DamageTypes => _damageList;

        public bool SupportsDamageClass(DamageGroupPrototype damageGroup)
        {
            return _supportedDamageGroups.Contains(damageGroup);
        }

        public bool SupportsDamageType(DamageTypePrototype type)
        {
            return _supportedDamageTypes.Contains(type);
        }

        public override void Initialize()
        {
            base.Initialize();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var damageType in prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
            {
                _damageTypeDict.Add(damageType.ID, damageType);
            }

            // TODO DAMAGE Serialize damage done and resistance changes
            var damageContainerPrototype = prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);

            _supportedDamageGroups.Clear();
            _supportedDamageTypes.Clear();

            DamageContainerId = damageContainerPrototype.ID;
            _supportedDamageGroups.UnionWith(damageContainerPrototype.SupportedClasses);
            _supportedDamageTypes.UnionWith(damageContainerPrototype.SupportedTypes);

            var resistancePrototype = prototypeManager.Index<ResistanceSetPrototype>(ResistanceSetId);
            Resistances = new ResistanceSet(resistancePrototype);
        }

        protected override void Startup()
        {
            base.Startup();

            ForceHealthChangedEvent();
        }

        public DamageTypePrototype GetDamageType(string ID)
        {
            return _damageTypeDict[ID];
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

        public int GetDamage(DamageGroupPrototype damageGroup)
        {
            if (!SupportsDamageClass(damageGroup))
            {
                return 0;
            }

            var damage = 0;

            foreach (var type in damageGroup.Types)
            {
                damage += GetDamage(type);
            }

            return damage;
        }

        public bool TryGetDamage(DamageGroupPrototype damageGroup, out int damage)
        {
            if (!SupportsDamageClass(damageGroup))
            {
                damage = 0;
                return false;
            }

            damage = GetDamage(damageGroup);
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

            if (_supportedDamageTypes.Contains(type))
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
            foreach (var type in _supportedDamageTypes)
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
            if (amount > 0)
            {
                return false;
            }

            if (!SupportsDamageType(type))
            {
                return false;
            }

            var finalDamage = amount;

            if (!ignoreResistances)
            {
                finalDamage = Resistances.CalculateDamage(type, amount);
            }

            if (!_damageList.TryGetValue(type, out var current))
            {
                return false;
            }

            _damageList[type] = current + finalDamage;

            if (_damageList[type] < 0)
            {
                _damageList[type] = 0;
                finalDamage = -current;
            }

            current = _damageList[type];

            var datum = new DamageChangeData(type, current, finalDamage);
            var data = new List<DamageChangeData> {datum};

            OnHealthChanged(data);

            return true;
        }

        public bool ChangeDamage(DamageGroupPrototype damageGroup, int amount, bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            if (amount > 0)
            {
                return false;
            }

            if (!SupportsDamageClass(damageGroup))
            {
                return false;
            }

            var types = damageGroup.Types;

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
                    if (healingLeft < types.Count())
                    {
                        // Say we were to distribute 2 healing between 3
                        // this will distribute 1 to each (and stop after 2 are given)
                        healPerType = 1;
                    }
                    else
                    {
                        // Say we were to distribute 62 healing between 3
                        // this will distribute 20 to each, leaving 2 for next loop
                        healPerType = healingLeft / types.Count();
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

                if (damageLeft < types.Count())
                {
                    damagePerType = 1;
                }
                else
                {
                    damagePerType = damageLeft / types.Count();
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
            if (newValue >= TotalDamage )
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

            if (Godmode)
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

            foreach (var type in _supportedDamageTypes)
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

        private IReadOnlyDictionary<DamageGroupPrototype, int> damageListToDamageGroup(IReadOnlyDictionary<DamageTypePrototype, int> damagelist)
        {
            var damageGroupDict = new Dictionary<DamageGroupPrototype, int>();
            int damageGroupSumDamage = 0;
            int damageTypeDamage = 0 ;
            foreach (var damageGroup in _supportedDamageGroups)
            {
                damageGroupSumDamage = 0;
                foreach (var damageType in _supportedDamageTypes)
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

        public DamageableComponentState(Dictionary<DamageTypePrototype, int> damageList ) : base(ContentNetIDs.DAMAGEABLE)
        {
            DamageList = damageList;
        }
    }
}
