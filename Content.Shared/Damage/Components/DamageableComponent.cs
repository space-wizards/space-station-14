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
    /// </summary>
    /// <remarks>
    ///     The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. DamageContainers
    ///     are effectively a dictionary of damage types and damage numbers, along with functions to modify them. Damage
    ///     groups are collections of damage types. A damage group is 'applicable' to a damageable component if it
    ///     supports at least one damage type in that group. A subset of these groups may be 'fully supported' when every
    ///     member of the group is supported by the container. This basic version never dies (thus can take an
    ///     indefinite amount of damage).
    /// </remarks>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    [NetworkedComponent()]
    public class DamageableComponent : Component, IDamageableComponent, IRadiationAct, ISerializationHooks
    {
        public override string Name => "Damageable";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>
        ///     The main damage dictionary. All the damage information is stored in this dictionary with <see cref="DamageTypePrototype"/>  keys.
        /// </summary>
        private Dictionary<DamageTypePrototype, int> _damageDict = new();

        [DataField("resistances")]
        public string ResistanceSetId { get; set; } = "defaultResistances";

        [ViewVariables] public ResistanceSet Resistances { get; set; } = new();

        // TODO DAMAGE Use as default values, specify overrides in a separate property through yaml for better (de)serialization
        [ViewVariables]
        [DataField("damageContainer")]
        public string DamageContainerId { get; set; } = "metallicDamageContainer";

        // TODO DAMAGE Cache this
        // When moving logic from damageableComponent --> Damage System, make damageSystem update these on damage change.
        [ViewVariables] public int TotalDamage => _damageDict.Values.Sum();
        [ViewVariables] public IReadOnlyDictionary<DamageTypePrototype, int> GetDamagePerType => _damageDict;
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> GetDamagePerApplicableGroup => DamageTypeDictToDamageGroupDict(_damageDict, ApplicableDamageGroups);
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> GetDamagePerFullySupportedGroup => DamageTypeDictToDamageGroupDict(_damageDict, FullySupportedDamageGroups);

        // Whenever sending over network, also need a <string, int> dictionary
        // TODO DAMAGE MAYBE Cache this?
        public IReadOnlyDictionary<string, int> GetDamagePerApplicableGroupIDs => ConvertDictKeysToIDs(GetDamagePerApplicableGroup);
        public IReadOnlyDictionary<string, int> GetDamagePerFullySupportedGroupIDs => ConvertDictKeysToIDs(GetDamagePerFullySupportedGroup);
        public IReadOnlyDictionary<string, int> GetDamagePerTypeIDs => ConvertDictKeysToIDs(_damageDict);

        // TODO PROTOTYPE Replace these datafield variables with prototype references, once they are supported.
        // Also requires appropriate changes in OnExplosion() and RadiationAct()
        [ViewVariables]
        [DataField("radiationDamageTypes")]
        public List<string> RadiationDamageTypeIDs { get; set; } = new() {"Radiation"};
        [ViewVariables]
        [DataField("explosionDamageTypes")]
        public List<string> ExplosionDamageTypeIDs { get; set; } = new() { "Piercing", "Heat" };

        public HashSet<DamageGroupPrototype> ApplicableDamageGroups { get; } = new();

        public HashSet<DamageGroupPrototype> FullySupportedDamageGroups { get; } = new();

        public HashSet<DamageTypePrototype> SupportedDamageTypes { get; } = new();

        protected override void Initialize()
        {
            base.Initialize();

            // TODO DAMAGE Serialize damage done and resistance changes
            var damageContainerPrototype = _prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);

            ApplicableDamageGroups.Clear();
            FullySupportedDamageGroups.Clear();
            SupportedDamageTypes.Clear();

            //Get Damage groups/types from the DamageContainerPrototype.
            DamageContainerId = damageContainerPrototype.ID;
            ApplicableDamageGroups.UnionWith(damageContainerPrototype.ApplicableDamageGroups);
            FullySupportedDamageGroups.UnionWith(damageContainerPrototype.FullySupportedDamageGroups);
            SupportedDamageTypes.UnionWith(damageContainerPrototype.SupportedDamageTypes);

            //initialize damage dictionary 0 damage
            _damageDict = new(SupportedDamageTypes.Count);
            foreach (var type in SupportedDamageTypes)
            {
                _damageDict.Add(type, 0);
            }

            Resistances = new ResistanceSet(_prototypeManager.Index<ResistanceSetPrototype>(ResistanceSetId));
        }

        protected override void Startup()
        {
            base.Startup();

            ForceHealthChangedEvent();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new DamageableComponentState(GetDamagePerTypeIDs);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is DamageableComponentState state))
            {
                return;
            }

            _damageDict.Clear();

            foreach (var (type, damage) in state.DamageDict)
            {
                _damageDict[_prototypeManager.Index<DamageTypePrototype>(type)] = damage;
            }
        }

        public int GetDamage(DamageTypePrototype type)
        {
            return GetDamagePerType.GetValueOrDefault(type);
        }

        public bool TryGetDamage(DamageTypePrototype type, out int damage)
        {
            return GetDamagePerType.TryGetValue(type, out damage);
        }

        public int GetDamage(DamageGroupPrototype group)
        {
            return GetDamagePerApplicableGroup.GetValueOrDefault(group);
        }

        public bool TryGetDamage(DamageGroupPrototype group, out int damage)
        {
            return GetDamagePerApplicableGroup.TryGetValue(group, out damage);
        }

        public bool IsApplicableDamageGroup(DamageGroupPrototype group)
        {
            return ApplicableDamageGroups.Contains(group);
        }

        public bool IsFullySupportedDamageGroup(DamageGroupPrototype group)
        {
            return FullySupportedDamageGroups.Contains(group);
        }

        public bool IsSupportedDamageType(DamageTypePrototype type)
        {
            return SupportedDamageTypes.Contains(type);
        }

        public bool TrySetDamage(DamageGroupPrototype group, int newValue)
        {
            if (!ApplicableDamageGroups.Contains(group))
            {
                return false;
            }

            if (newValue < 0)
            {
                // invalid value
                return false;
            }

            foreach (var type in group.DamageTypes)
            {
                TrySetDamage(type, newValue);
            }
            return true;
        }

        public bool TrySetAllDamage(int newValue)
        {
            if (newValue < 0)
            {
                // invalid value
                return false;
            }

            foreach (var type in SupportedDamageTypes)
            {
                TrySetDamage(type, newValue);
            }

            return true;
        }

        public bool TryChangeDamage(DamageTypePrototype type, int amount, bool ignoreDamageResistances = false)
        {
            // Check if damage type is supported, and get the current value if it is.
            if (!GetDamagePerType.TryGetValue(type, out var current))
            {
                return false;
            }

            if (amount == 0)
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
                    return false;

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

        public bool TryChangeDamage(DamageGroupPrototype group, int amount, bool ignoreDamageResistances = false)
        {
            var types = group.DamageTypes.ToArray();

            if (amount < 0)
            {
                // We are Healing. Keep track of how much we can hand out (with a better var name for readability).
                var availableHealing = -amount;

                // Get total group damage.
                var damageToHeal = GetDamagePerApplicableGroup[group];

                // Is there any damage to even heal?
                if (damageToHeal == 0)
                    return false;

                // If total healing is more than there is damage, just set to 0 and return.
                if (damageToHeal <= availableHealing)
                {
                    TrySetDamage(group, 0);
                    return true;
                }

                // Partially heal each damage group
                int healing, damage;
                foreach (var type in types)
                {
                    if (!_damageDict.TryGetValue(type, out damage))
                    {
                        // Damage Type is not supported. Continue without reducing availableHealing
                        continue;
                    }

                    // Apply healing to the damage type. The healing amount may be zero if either damage==0, or if
                    // integer rounding made it zero (i.e., damage is small)
                    healing = (availableHealing * damage) / damageToHeal;
                    TryChangeDamage(type, -healing, ignoreDamageResistances);

                    // remove this damage type from the damage we consider for future loops, regardless of how much we
                    // actually healed this type.
                    damageToHeal -= damage;
                    availableHealing -= healing;

                    // If we now healed all the damage, exit. otherwise 1/0 and universe explodes.
                    if (damageToHeal == 0)
                    {
                        break;
                    }
                }

                // Damage type is supported, there was damage to heal, and resistances were ignored
                // --> Damage must have changed
                return true;
            }
            else if (amount > 0)
            {
                // Resistances may result in no actual damage change. We need to keep track if any damage got through.
                var damageChanged = false;

                // We are adding damage. Keep track of how much we can dish out (with a better var name for readability).
                var availableDamage = amount;

                // How many damage types do we have to distribute over?.
                var numberDamageTypes = types.Length;

                // Apply damage to each damage group
                int damage;
                foreach (var type in types)
                {
                    // Distribute the remaining damage over the remaining damage types.
                    damage = availableDamage / numberDamageTypes;

                    // Try apply the damage type. If damage type is not supported, this has no effect.
                    // We also use the return value to check whether any damage has changed
                    damageChanged = TryChangeDamage(type, damage, ignoreDamageResistances) || damageChanged;

                    // regardless of whether we dealt damage, reduce the amount to distribute.
                    availableDamage -= damage;
                    numberDamageTypes -= 1;

                }
                return damageChanged;
            }

            // amount==0 no damage change.
            return false;
        }

        public bool TrySetDamage(DamageTypePrototype type, int newValue)
        {
            if (!_damageDict.TryGetValue(type, out var oldValue))
            {
                return false;
            }

            if (newValue < 0)
            {
                // invalid value
                return false;
            }

            if (oldValue == newValue)
            {
                // No health change.
                // But we are trying to set, not trying to change.
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

            foreach (var typeID in RadiationDamageTypeIDs)
            {
                TryChangeDamage(_prototypeManager.Index<DamageTypePrototype>(typeID), totalDamage);
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

            foreach (var typeID in ExplosionDamageTypeIDs)
            {
                TryChangeDamage(_prototypeManager.Index<DamageTypePrototype>(typeID), damage);
            }
        }

        /// <summary>
        ///     Take a dictionary with <see cref="IPrototype"/> keys and return a dictionary using <see cref="IPrototype.ID"/> as keys
        ///     instead.
        /// </summary>
        /// <remarks>
        ///     Useful when sending damage type and group prototypes dictionaries over the network.
        /// </remarks>
        public static IReadOnlyDictionary<string, int>
            ConvertDictKeysToIDs<TPrototype>(IReadOnlyDictionary<TPrototype, int> prototypeDict)
            where TPrototype : IPrototype
        {
            Dictionary<string, int> idDict = new(prototypeDict.Count);
            foreach (var entry in prototypeDict)
            {
                idDict.Add(entry.Key.ID, entry.Value);
            }
            return idDict;
        }

        /// <summary>
        ///     Convert a dictionary with damage type keys to a dictionary of damage groups keys.
        /// </summary>
        /// <remarks>
        ///     Takes a dictionary with damage types as keys and integers as values, and an iterable list of damage
        ///     groups. Returns a dictionary with damage group keys, with values calculated by adding up the values for
        ///     each damage type in that group. If a damage type is associated with more than one supported damage
        ///     group, it will contribute to the total of each group. Conversely, some damage types may not contribute
        ///     to the new dictionary if their associated group(s) are not in given list of groups.
        /// </remarks>
        public static IReadOnlyDictionary<DamageGroupPrototype, int>
            DamageTypeDictToDamageGroupDict(IReadOnlyDictionary<DamageTypePrototype, int> damageTypeDict, IEnumerable<DamageGroupPrototype> groupKeys)
        {
            var damageGroupDict = new Dictionary<DamageGroupPrototype, int>();
            int damageGroupSumDamage, damageTypeDamage;
            // iterate over the list of group keys for our new dictionary
            foreach (var group in groupKeys)
            {
                // For each damage type in this group, add up the damage present in the given dictionary
                damageGroupSumDamage = 0;
                foreach (var type in group.DamageTypes)
                {
                    // if the damage type is in the dictionary, add it's damage to the group total.
                    if (damageTypeDict.TryGetValue(type, out damageTypeDamage))
                    {
                        damageGroupSumDamage += damageTypeDamage;
                    }
                }
                damageGroupDict.Add(group, damageGroupSumDamage);
            }
            return damageGroupDict;
        }

    }

    [Serializable, NetSerializable]
    public class DamageableComponentState : ComponentState
    {
        public readonly IReadOnlyDictionary<string, int> DamageDict;

        public DamageableComponentState(IReadOnlyDictionary<string, int> damageDict) 

        {
            DamageDict = damageDict;
        }
    }
}
