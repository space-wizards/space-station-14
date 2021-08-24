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
<<<<<<< refs/remotes/origin/master

<<<<<<< refs/remotes/origin/master
=======

=======
>>>>>>> fix a few bugs
        public override string Name => "Damageable";
        public override uint? NetID => ContentNetIDs.DAMAGEABLE;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
<<<<<<< HEAD

<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
>>>>>>> update damagecomponent across shared and server
=======
>>>>>>> Fix Merge issues
        // TODO define these in yaml?
        public const string DefaultResistanceSet = "defaultResistances";
        public const string DefaultDamageContainer = "metallicDamageContainer";
=======
        /// <summary>
        ///     The main damage dictionary. All the damage information is stored in this dictionary with <see cref="DamageTypePrototype"/>  keys.
        /// </summary>
        private Dictionary<DamageTypePrototype, int> _damageDict = new();
>>>>>>> Refactor damageablecomponent update (#4406)

<<<<<<< refs/remotes/origin/master
        private readonly Dictionary<DamageType, int> _damageList = DamageTypeExtensions.ToNewDictionary();

        [DataField("resistances")] public string ResistanceSetId = DefaultResistanceSet;
=======
        [DataField("resistances")]
        public string ResistanceSetId { get; set; } = "defaultResistances";

        [ViewVariables] public ResistanceSet Resistances { get; set; } = new();
>>>>>>> Merge fixes

        // TODO DAMAGE Use as default values, specify overrides in a separate property through yaml for better (de)serialization
<<<<<<< refs/remotes/origin/master
        [ViewVariables] [DataField("damageContainer")] public string DamageContainerId { get; set; } = DefaultDamageContainer;
=======

        /// <summary>
        ///     The main damage dictionary. All the damage information is stored in this dictionary with <see cref="DamageTypePrototype"/>  keys.
        /// </summary>
        private Dictionary<DamageTypePrototype, int> _damageDict = new();

        [DataField("resistances")]
        public string ResistanceSetId { get; set; } = "defaultResistances";
>>>>>>> refactor-damageablecomponent

        [ViewVariables] public ResistanceSet Resistances { get; set; } = new();
=======
        [ViewVariables]
        [DataField("damageContainer")]
<<<<<<< refs/remotes/origin/master
        public string DamageContainerId { get; set; } = DefaultDamageContainer;
>>>>>>> fix a few bugs

        // TODO DAMAGE Use as default values, specify overrides in a separate property through yaml for better (de)serialization
        [ViewVariables]
        [DataField("damageContainer")]
        public string DamageContainerId { get; set; } = "metallicDamageContainer";

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        [ViewVariables] public IReadOnlyDictionary<DamageClass, int> DamageClasses => _damageList.ToClassDictionary();
=======
        // TODO DAMAGE Cache this
        // When moving logic from damageableComponent --> Damage System, make damageSystem update these on damage change.
        [ViewVariables] public int TotalDamage => _damageDict.Values.Sum();
        [ViewVariables] public IReadOnlyDictionary<DamageTypePrototype, int> GetDamagePerType => _damageDict;
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> GetDamagePerApplicableGroup => DamageTypeDictToDamageGroupDict(_damageDict, ApplicableDamageGroups);
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> GetDamagePerFullySupportedGroup => DamageTypeDictToDamageGroupDict(_damageDict, FullySupportedDamageGroups);
>>>>>>> refactor-damageablecomponent

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

<<<<<<< HEAD
        public bool SupportsDamageClass(DamageClass @class)
        {
            return SupportedClasses.Contains(@class);
=======
        public HashSet<DamageGroupPrototype> SupportedGroups { get; } = new();
=======
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
>>>>>>> Refactor damageablecomponent update (#4406)

        public HashSet<DamageGroupPrototype> FullySupportedDamageGroups { get; } = new();

        public HashSet<DamageTypePrototype> SupportedDamageTypes { get; } = new();

<<<<<<< refs/remotes/origin/master
        public bool SupportsDamageClass(DamageGroupPrototype damageGroup)
        {
            return SupportedGroups.Contains(damageGroup);
>>>>>>> Merge fixes
        }
=======
        public HashSet<DamageGroupPrototype> FullySupportedDamageGroups { get; } = new();
>>>>>>> refactor-damageablecomponent

        public HashSet<DamageTypePrototype> SupportedDamageTypes { get; } = new();

=======
>>>>>>> update damagecomponent across shared and server
        protected override void Initialize()
        {
            base.Initialize();
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

=======
>>>>>>> refactor-damageablecomponent
            // TODO DAMAGE Serialize damage done and resistance changes
            var damageContainerPrototype = _prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);

            ApplicableDamageGroups.Clear();
            FullySupportedDamageGroups.Clear();
            SupportedDamageTypes.Clear();

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            SupportedClasses.Clear();
            SupportedTypes.Clear();
=======
=======
>>>>>>> fix a few bugs
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
=======
>>>>>>> Refactor damageablecomponent update (#4406)

            // TODO DAMAGE Serialize damage done and resistance changes
            var damageContainerPrototype = _prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);
>>>>>>> update damagecomponent across shared and server

<<<<<<< refs/remotes/origin/master
            DamageContainerId = damagePrototype.ID;
            SupportedClasses.UnionWith(damagePrototype.SupportedClasses);
            SupportedTypes.UnionWith(damagePrototype.SupportedTypes);
=======
            SupportedGroups.Clear();
            SupportedTypes.Clear();
=======
            ApplicableDamageGroups.Clear();
            FullySupportedDamageGroups.Clear();
            SupportedDamageTypes.Clear();
>>>>>>> Refactor damageablecomponent update (#4406)

            //Get Damage groups/types from the DamageContainerPrototype.
            DamageContainerId = damageContainerPrototype.ID;
<<<<<<< refs/remotes/origin/master
            SupportedGroups.UnionWith(damageContainerPrototype.SupportedDamageGroups);
            SupportedTypes.UnionWith(damageContainerPrototype.SupportedDamageTypes);
>>>>>>> Merge fixes
=======
            ApplicableDamageGroups.UnionWith(damageContainerPrototype.ApplicableDamageGroups);
            FullySupportedDamageGroups.UnionWith(damageContainerPrototype.FullySupportedDamageGroups);
            SupportedDamageTypes.UnionWith(damageContainerPrototype.SupportedDamageTypes);
>>>>>>> Refactor damageablecomponent update (#4406)

=======
            //Get Damage groups/types from the DamageContainerPrototype.
            DamageContainerId = damageContainerPrototype.ID;
            ApplicableDamageGroups.UnionWith(damageContainerPrototype.ApplicableDamageGroups);
            FullySupportedDamageGroups.UnionWith(damageContainerPrototype.FullySupportedDamageGroups);
            SupportedDamageTypes.UnionWith(damageContainerPrototype.SupportedDamageTypes);

>>>>>>> refactor-damageablecomponent
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

<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
=======
        public DamageTypePrototype GetDamageType(string ID)
        {
            return _prototypeManager.Index<DamageTypePrototype>(ID);
        }

        public DamageGroupPrototype GetDamageGroup(string ID)
        {
            return _prototypeManager.Index<DamageGroupPrototype>(ID);
        }

>>>>>>> update damagecomponent across shared and server
=======
>>>>>>> Refactor damageablecomponent update (#4406)
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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        public int GetDamage(DamageClass @class)
        {
            if (!SupportsDamageClass(@class))
            {
                return 0;
            }
=======
        public int GetDamage(DamageGroupPrototype group)
        {
            return GetDamagePerApplicableGroup.GetValueOrDefault(group);
        }
>>>>>>> Refactor damageablecomponent update (#4406)
=======
        public int GetDamage(DamageGroupPrototype group)
        {
            return GetDamagePerApplicableGroup.GetValueOrDefault(group);
        }
>>>>>>> refactor-damageablecomponent

        public bool TryGetDamage(DamageGroupPrototype group, out int damage)
        {
            return GetDamagePerApplicableGroup.TryGetValue(group, out damage);
        }

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            foreach (var type in @class.ToTypes())
            {
                damage += GetDamage(type);
            }
=======
=======
>>>>>>> refactor-damageablecomponent
        public bool IsApplicableDamageGroup(DamageGroupPrototype group)
        {
            return ApplicableDamageGroups.Contains(group);
        }

        public bool IsFullySupportedDamageGroup(DamageGroupPrototype group)
        {
            return FullySupportedDamageGroups.Contains(group);
<<<<<<< HEAD
=======
        }

        public bool IsSupportedDamageType(DamageTypePrototype type)
        {
            return SupportedDamageTypes.Contains(type);
>>>>>>> refactor-damageablecomponent
        }
>>>>>>> Refactor damageablecomponent update (#4406)

<<<<<<< HEAD
        public bool IsSupportedDamageType(DamageTypePrototype type)
        {
            return SupportedDamageTypes.Contains(type);
        }

<<<<<<< refs/remotes/origin/master
        public bool TryGetDamage(DamageClass @class, out int damage)
        {
            if (!SupportsDamageClass(@class))
=======
        public bool TrySetDamage(DamageGroupPrototype group, int newValue)
        {
            if (!ApplicableDamageGroups.Contains(group))
>>>>>>> Refactor damageablecomponent update (#4406)
=======
        public bool TrySetDamage(DamageGroupPrototype group, int newValue)
        {
            if (!ApplicableDamageGroups.Contains(group))
>>>>>>> refactor-damageablecomponent
            {
                return false;
            }

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
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
=======
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
>>>>>>> Refactor damageablecomponent update (#4406)
=======
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
>>>>>>> refactor-damageablecomponent
        {
            if (newValue < 0)
            {
                // invalid value
                return false;
            }

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
            var damageClass = type.ToClass();

            if (SupportedClasses.Contains(damageClass))
=======
            if (SupportedTypes.Contains(type))
>>>>>>> Merge fixes
=======
            foreach (var type in SupportedDamageTypes)
>>>>>>> Refactor damageablecomponent update (#4406)
=======
            foreach (var type in SupportedDamageTypes)
>>>>>>> refactor-damageablecomponent
            {
                TrySetDamage(type, newValue);
            }

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            return false;
        }

        public void Heal(DamageType type)
        {
            SetDamage(type, 0);
=======
            return true;
>>>>>>> Refactor damageablecomponent update (#4406)
=======
            return true;
>>>>>>> refactor-damageablecomponent
        }

        public bool TryChangeDamage(DamageTypePrototype type, int amount, bool ignoreDamageResistances = false)
        {
            // Check if damage type is supported, and get the current value if it is.
            if (!GetDamagePerType.TryGetValue(type, out var current))
            {
                return false;
            }

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        public bool ChangeDamage(
            DamageType type,
            int amount,
            bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            if (!SupportsDamageType(type))
=======
            if (amount == 0)
>>>>>>> Refactor damageablecomponent update (#4406)
=======
            if (amount == 0)
>>>>>>> refactor-damageablecomponent
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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        public bool ChangeDamage(DamageClass @class, int amount, bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            if (!SupportsDamageClass(@class))
            {
                return false;
            }

<<<<<<< refs/remotes/origin/master
            var types = @class.ToTypes();
=======
            var types = damageGroup.DamageTypes.ToArray();
>>>>>>> Merge fixes
=======
        public bool TryChangeDamage(DamageGroupPrototype group, int amount, bool ignoreDamageResistances = false)
        {
            var types = group.DamageTypes.ToArray();
>>>>>>> Refactor damageablecomponent update (#4406)
=======
        public bool TryChangeDamage(DamageGroupPrototype group, int amount, bool ignoreDamageResistances = false)
        {
            var types = group.DamageTypes.ToArray();
>>>>>>> refactor-damageablecomponent

            if (amount < 0)
            {
                // We are Healing. Keep track of how much we can hand out (with a better var name for readability).
                var availableHealing = -amount;

                // Get total group damage.
                var damageToHeal = GetDamagePerApplicableGroup[group];
<<<<<<< HEAD

                // Is there any damage to even heal?
                if (damageToHeal == 0)
                    return false;

=======

                // Is there any damage to even heal?
                if (damageToHeal == 0)
                    return false;

>>>>>>> refactor-damageablecomponent
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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
        public bool SetDamage(DamageType type, int newValue, IEntity? source = null,  DamageChangeParams? extraParams = null)
=======
        public bool SetDamage(DamageTypePrototype type, int newValue, IEntity? source = null, DamageChangeParams? extraParams = null)
>>>>>>> Fix Merge issues
=======
        public bool TrySetDamage(DamageTypePrototype type, int newValue)
>>>>>>> Refactor damageablecomponent update (#4406)
=======
        public bool TrySetDamage(DamageTypePrototype type, int newValue)
>>>>>>> refactor-damageablecomponent
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

<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
=======
        private IReadOnlyDictionary<DamageGroupPrototype, int> damageListToDamageGroup(IReadOnlyDictionary<DamageTypePrototype, int> damagelist)
        {
            var damageGroupDict = new Dictionary<DamageGroupPrototype, int>();
            int damageGroupSumDamage = 0;
            int damageTypeDamage = 0;
            foreach (var damageGroup in SupportedGroups)
            {
                damageGroupSumDamage = 0;
                foreach (var damageType in SupportedTypes)
                {
                    damageTypeDamage = 0;
                    damagelist.TryGetValue(damageType, out damageTypeDamage);
                    damageGroupSumDamage += damageTypeDamage;
                }
                damageGroupDict.Add(damageGroup, damageGroupSumDamage);
            }

            return damageGroupDict;
        }

>>>>>>> Merge fixes
=======
>>>>>>> Refactor damageablecomponent update (#4406)
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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            ChangeDamage(DamageType.Radiation, totalDamage, false, radiation.Owner);
=======
=======
>>>>>>> refactor-damageablecomponent
            foreach (var typeID in RadiationDamageTypeIDs)
            {
                TryChangeDamage(_prototypeManager.Index<DamageTypePrototype>(typeID), totalDamage);
            }
            
<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent
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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            ChangeDamage(DamageType.Piercing, damage, false);
            ChangeDamage(DamageType.Heat, damage, false);
=======
=======
>>>>>>> refactor-damageablecomponent
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
<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent
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
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        public readonly Dictionary<DamageType, int> DamageList;

        public DamageableComponentState(Dictionary<DamageType, int> damageList)
=======
        public readonly IReadOnlyDictionary<string, int> DamageDict;

        public DamageableComponentState(IReadOnlyDictionary<string, int> damageDict) 

>>>>>>> Refactor damageablecomponent update (#4406)
=======
        public readonly IReadOnlyDictionary<string, int> DamageDict;

        public DamageableComponentState(IReadOnlyDictionary<string, int> damageDict) 

>>>>>>> refactor-damageablecomponent
        {
            DamageDict = damageDict;
        }
    }
}
