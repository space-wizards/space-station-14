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

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private Dictionary<DamageTypePrototype, int> _damageDict = new();

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
        [ViewVariables] public int TotalDamage => _damageDict.Values.Sum();
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> DamageGroups => DamageGroupPrototype.DamageTypeDictToDamageGroupDict(_damageDict, ApplicableDamageGroups);
        [ViewVariables] public IReadOnlyDictionary<DamageTypePrototype, int> DamageTypes => _damageDict;

        // TODO DAMAGE Cache this
        // Whenever sending over network, need a <string, int> dictionary
        public IReadOnlyDictionary<string, int> DamageGroupIDs => ConvertDictKeysToIDs(DamageGroups);
        public IReadOnlyDictionary<string, int> DamageTypeIDs => ConvertDictKeysToIDs(DamageTypes);

        // Some inorganic damageable components might take shock/electrical damage from radiation?
        // Similarly, some may react differetly to explosions?
        // There definittely should be a better way of doing this.
        // TODO PROTOTYPE Replace these datafield variables with prototype references, once they are supported.
        // This also requires changing the list type and modifying the functions here that use them.
        [ViewVariables]
        [DataField("radiationDamageTypes")]
        public List<string> RadiationDamageTypeIDs { get; set; } = new() {"Radiation"};
        [ViewVariables]
        [DataField("explosionDamageTypes")]
        public List<string> ExplosionDamageTypeIDs { get; set; } = new() { "Piercing", "Heat" };


        public HashSet<DamageGroupPrototype> ApplicableDamageGroups { get; } = new();

        public HashSet<DamageTypePrototype> SupportedDamageTypes { get; } = new();

        protected override void Initialize()
        {
            base.Initialize();

            // TODO DAMAGE Serialize damage done and resistance changes
            var damageContainerPrototype = _prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);

            ApplicableDamageGroups.Clear();
            SupportedDamageTypes.Clear();

            DamageContainerId = damageContainerPrototype.ID;
            ApplicableDamageGroups.UnionWith(damageContainerPrototype.ApplicableDamageGroups);
            SupportedDamageTypes.UnionWith(damageContainerPrototype.SupportedDamageTypes);

            foreach (var DamageType in SupportedDamageTypes)
            {
                _damageDict.Add(DamageType,0);
            }

            var resistancePrototype = _prototypeManager.Index<ResistanceSetPrototype>(ResistanceSetId);
            Resistances = new ResistanceSet(resistancePrototype);
        }

        public bool SupportsDamageGroup(DamageGroupPrototype group)
        {
            return ApplicableDamageGroups.Contains(group);
        }

        public bool SupportsDamageType(DamageTypePrototype type)
        {
            return SupportedDamageTypes.Contains(type);
        }

        protected override void Startup()
        {
            base.Startup();

            ForceHealthChangedEvent();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new DamageableComponentState(_damageDict);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is DamageableComponentState state))
            {
                return;
            }

            _damageDict.Clear();

            foreach (var (type, damage) in state.DamageList)
            {
                _damageDict[type] = damage;
            }
        }

        public int GetDamage(DamageTypePrototype type)
        {
            return _damageDict.GetValueOrDefault(type);
        }

        public bool TryGetDamage(DamageTypePrototype type, out int damage)
        {
            return _damageDict.TryGetValue(type, out damage);
        }

        public int GetDamage(DamageGroupPrototype group)
        {
            if (!SupportsDamageGroup(group))
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
            if (!SupportsDamageGroup(group))
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

            if (SupportedDamageTypes.Contains(type))
            {
                var old = _damageDict[type] = newValue;
                _damageDict[type] = newValue;

                var delta = newValue - old;
                var datum = new DamageChangeData(type, newValue, delta);
                var data = new List<DamageChangeData> {datum};

                OnHealthChanged(data);

                return true;
            }

            return false;
        }

        public void SetGroupDamage(int newValue, DamageGroupPrototype group)
        {
            foreach (var type in group.DamageTypes)
            {
                SetDamage(type, newValue);
            }
        }

        public void SetAllDamage(int newValue)
        {
            foreach (var type in SupportedDamageTypes)
            {
                SetDamage(type, newValue);
            }
        }

        // TODO QUESTION both source and extraParams are unused here. Should they be removed, or will they have use in the future?
        public bool ChangeDamage(
            DamageTypePrototype type,
            int amount,
            bool ignoreDamageResistances = false,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            // Check if damage type is supported, and get the current value if it is.
            if (amount == 0 || !_damageDict.TryGetValue(type, out var current))
            {
                return false;
            }

            // Apply resistances (does nothing if amount<0)
            var finalDamage = amount;
            if (!ignoreDamageResistances)
            {
                finalDamage = Resistances.CalculateDamage(type, amount);
            }

            if (finalDamage == 0)
                return false;

            // Are we healing below zero?
            if (current + finalDamage < 0)
            {
                if (current == 0)
                    // Damage type is supported, but there is nothing to do
                    return true;

                // Cap healing down to zero
                _damageDict[type] = 0;
                finalDamage = -current;
            }
            else
            {
                _damageDict[type] = current + finalDamage;
            }

            current = _damageDict[type];

            var datum = new DamageChangeData(type, current, finalDamage);
            var data = new List<DamageChangeData> {datum};

            OnHealthChanged(data);

            return true;
        }

        public bool ChangeDamage(DamageGroupPrototype group, int amount, bool ignoreDamageResistances = false,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            if (!SupportsDamageGroup(group))
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

                        ChangeDamage(type, -healAmount, ignoreDamageResistances, source, extraParams);
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
                    ChangeDamage(type, damageAmount, ignoreDamageResistances, source, extraParams);
                    damageLeft -= damageAmount;
                }
            }

            return true;
        }

        public bool SetDamage(DamageTypePrototype type, int newValue, IEntity? source = null,  DamageChangeParams? extraParams = null)
        {
            // TODO QUESTION what is this if statement supposed to do?
            // Is TotalDamage supposed to be something like MaxDamage? I don't think DamageableComponents has a MaxDamage?
            if (newValue >= TotalDamage)
            {
                return false;
            }

            if (newValue < 0)
            {
                return false;
            }

            if (!_damageDict.TryGetValue(type, out var oldValue))
            {
                return false;
            }

            if (oldValue == newValue)
            {
                // Dont bother calling OnHealthChanged(data).
                return true;
            }

            _damageDict[type] = newValue;

            var delta = newValue - oldValue;
            var datum = new DamageChangeData(type, 0, delta);
            var data = new List<DamageChangeData> {datum};

            OnHealthChanged(data);

            return true;
        }

        public void ForceHealthChangedEvent()
        {
            var data = new List<DamageChangeData>();

            foreach (var type in SupportedDamageTypes)
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

        public void RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            var totalDamage = Math.Max((int)(frameTime * radiation.RadsPerSecond), 1);

            foreach (string damageTypeID in RadiationDamageTypeIDs)
            {
                ChangeDamage(_prototypeManager.Index<DamageTypePrototype>(damageTypeID), totalDamage, false, radiation.Owner);
            }
            
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

            foreach (string damageTypeID in ExplosionDamageTypeIDs)
            {
                ChangeDamage(_prototypeManager.Index<DamageTypePrototype>(damageTypeID), damage, false);
            }
        }

        // TODO This probably does not belong here? Should this be a PrototypeManager function?
        /// <summary>
        /// Take a dictionary with protoype keys, and return a dictionary using the prototype ID strings as keys instead.
        /// Usefull when sending prototypes dictionaries over the network.
        /// </summary>
        public static IReadOnlyDictionary<string, TValue>
            ConvertDictKeysToIDs<TPrototype,TValue>(IReadOnlyDictionary<TPrototype, TValue> prototypeDict) where TPrototype : IPrototype
        {
            Dictionary<string, TValue> idDict = new(prototypeDict.Count);
            foreach (var entry in prototypeDict)
            {
                idDict.Add(entry.Key.ID, entry.Value);
            }
            return idDict;
        }

        // TODO This probably does not belong here? Should this be a PrototypeManager function?
        /// <summary>
        /// Takes a dictionary with strings as keys.
        /// Find prototypes with matching IDs using the prototype manager.
        /// Returns a dictionary with the ID strings replaced by prototypes.
        /// Usefull when receiving prototypes dictionaries over the network.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if one of the string IDs does not exist.
        /// </exception>
        public IReadOnlyDictionary<TPrototype, TValue>
            ConvertDictKeysToPrototypes<TPrototype, TValue>(IReadOnlyDictionary<string, TValue> stringDict)
            where TPrototype : class, IPrototype
        {
            Dictionary<TPrototype, TValue> prototypeDict = new(stringDict.Count);
            foreach (var entry in stringDict)
            {
                prototypeDict.Add(_prototypeManager.Index<TPrototype>(entry.Key), entry.Value);
            }
            return prototypeDict;
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
