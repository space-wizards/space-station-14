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

        // TODO DAMAGE Cache this, refresh on DamageChange() or DamageSet()
        [ViewVariables] public int TotalDamage => _damageDict.Values.Sum();
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> DamagePerGroup => DamageGroupPrototype.DamageTypeDictToDamageGroupDict(_damageDict, ApplicableDamageGroups);
        [ViewVariables] public IReadOnlyDictionary<DamageGroupPrototype, int> DamagePerSupportedGroup => DamageGroupPrototype.DamageTypeDictToDamageGroupDict(_damageDict, SupportedDamageGroups);
        [ViewVariables] public IReadOnlyDictionary<DamageTypePrototype, int> DamagePerType => _damageDict;

        // TODO DAMAGE Cache this, refresh on DamageChange() or DamageSet()
        // Whenever sending over network, need a <string, int> dictionary
        public IReadOnlyDictionary<string, int> DamagePerGroupIDs => ConvertDictKeysToIDs(DamagePerGroup);
        public IReadOnlyDictionary<string, int> DamagePerSupportedGroupIDs => ConvertDictKeysToIDs(DamagePerSupportedGroup);
        public IReadOnlyDictionary<string, int> DamagePerTypeIDs => ConvertDictKeysToIDs(DamagePerType);

        // TODO QUESTIONS This is how we are currently specifying the effects of explosions and radiation. The damage
        // type was hard coded, now its a yaml datafield as recommended. However, as DrSmugleaf said, "There should be a
        // better way of doing this". I think there is, and have some comments in the damage.yml file, though maybe
        // those are controversial oppinions.
        [ViewVariables]
        [DataField("radiationDamageTypes")]
        public List<string> RadiationDamageTypeIDs { get; set; } = new() {"Radiation"};
        [ViewVariables]
        [DataField("explosionDamageTypes")]
        public List<string> ExplosionDamageTypeIDs { get; set; } = new() { "Piercing", "Heat" };

        public HashSet<DamageGroupPrototype> ApplicableDamageGroups { get; } = new();

        public HashSet<DamageGroupPrototype> SupportedDamageGroups { get; } = new();

        public HashSet<DamageTypePrototype> SupportedDamageTypes { get; } = new();

        protected override void Initialize()
        {
            base.Initialize();

            // TODO DAMAGE Serialize damage done and resistance changes
            var damageContainerPrototype = _prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);

            ApplicableDamageGroups.Clear();
            SupportedDamageGroups.Clear();
            SupportedDamageTypes.Clear();

            //Get Damage groups/types from the DamageContainerPrototype.
            DamageContainerId = damageContainerPrototype.ID;
            ApplicableDamageGroups.UnionWith(damageContainerPrototype.ApplicableDamageGroups);
            SupportedDamageGroups.UnionWith(damageContainerPrototype.SupportedDamageGroups);
            SupportedDamageTypes.UnionWith(damageContainerPrototype.SupportedDamageTypes);

            //initialise damage dictionary 0 damage
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


        // TODO QUESTION These four functions here are just wrapping standard function calls on public dictionaries. I'm
        // not sure whether or not they should just be replaced by those dictionary calls. The current function names
        // probably make for nicer to read code.
        public int GetDamage(DamageTypePrototype type)
        {
            return DamagePerType.GetValueOrDefault(type);
        }

        public bool TryGetDamage(DamageTypePrototype type, out int damage)
        {
            return DamagePerType.TryGetValue(type, out damage);
        }

        public int GetDamage(DamageGroupPrototype group)
        {
            return DamagePerGroup.GetValueOrDefault(group);
        }

        public bool TryGetDamage(DamageGroupPrototype group, out int damage)
        {
            return DamagePerGroup.TryGetValue(group, out damage);
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

        // TODO QUESTION both source and extraParams are unused here. Should they be removed, or will they have use in
        // the future? The documentation for IDamageableComponent.SetDamage() mentions it would be used for targeting
        // limbs and such. But so far I've been under the assumption that each limb/organ would have it's own
        // DamageableComponent, and limb targeting would be elsewhere?
        public bool ChangeDamage(
            DamageTypePrototype type,
            int amount,
            bool ignoreDamageResistances = false,
            IEntity? source = null,
            DamageChangeParams? extraParams = null)
        {
            // Check if damage type is supported, and get the current value if it is.
            if (!_damageDict.TryGetValue(type, out var current))
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            // Apply resistances (does nothing if amount<0)
            var finalDamage = amount;
            if (!ignoreDamageResistances)
            {
                finalDamage = Resistances.CalculateDamage(type, amount);
            }

            if (finalDamage == 0)
                return true;

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
            if (!ApplicableDamageGroups.Contains(group))
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


        // TODO QUESTION Please dont run away, its a very interesting wall of text.
        // Below is an alternative version of ChangeDamage(DamageGroupPrototype). Currently for a damge
        // group with n types, ChangeDamage() can call ChangeDamage(DamageTypePrototype) up to 2*n-1 times. As this
        // function has a not-insignificant bit of logic, and I think uses some sort of networking/messaging we probably
        // want to minimise that down to n (or 1 if somehow possible with networking). In the case where all of the
        // damage is of one type, reducing it to 1 is trivial.
        //
        // Additionally currently ChangeDamage will ignore a damageType if it is not supported. As a result, the actual
        // amount by which the total grouip damage changes may be less than expected. I think this is a good thing for
        // dealing damage: if a damageContainer is 'immune' to some of the damage in the group, it should take less
        // damage.
        //
        // On the other hand, I feel that when a doctor injects a patient with 1u drug that should heal 10 damage in a
        // damage group, they expect it to do so. The total health change should be the same, regardless of whether a
        // damage type is supported, or whether a damge type is already set to zero. Otherwise a doctor could see a
        // patient with brute, and try a brute drug, only to have it work at 1/3 effectivness because slash and piercing
        // are already at full health. Currently, this is also how ChangeDamage behaves.
        //
        // So below is an alternative version of ChangeDamage(DamageGroupPrototype) that keeps the same behaviour
        // outlined above, but uses less ChangeDamage(DamageTypePrototype) calls. It does change healing behaviour: the
        // ammount that each damage type is healed by is proportional to current damage in that type relative to the
        // group.
        //
        // So for example, consider someone with Blun/Slash/Piercing damage of 20/20/10.
        // If you heal for 31 Brute using this code, you would heal for 12/12/7.
        // This wasn't an intentional design decision, it just so happens that this is the easiest algorithm I can think
        // of that minimises calls to ChangeDamage(damageType), while also not wasting any healing. Although, I do
        // actually like this behaviour.
        public void ChangeDamageAlternative(DamageGroupPrototype group, int amount, bool ignoreDamageResistances = false,
    IEntity? source = null,
    DamageChangeParams? extraParams = null)
        {

            var types = group.DamageTypes.ToArray();

            if (amount < 0)
            {
                // We are Healing. Keep track of how much we can hand out (with a better var name for readbility).
                var availableHealing = -amount;

                // Get total group damage.
                var damageToHeal = DamagePerGroup[group];

                // Is there any damage to even heal?
                if (damageToHeal == 0)
                    return;

                // If total healing is more than there is damage, just set to 0 and return.
                if (damageToHeal <= availableHealing)
                    SetGroupDamage(0, group);

                // Partially heal each damage group
                int healing;
                int damage;
                foreach (var type in types)
                {

                    if (!DamagePerType.TryGetValue(type, out damage))
                    {
                        // Damage Type is not supported. Continue without reducing availableHealing
                        continue;
                    }

                    // Apply healing to the damage type. The healing amount may be zero if either damage==0, or if
                    // integer rounding made it zero (i.e., damage is small)
                    healing = (availableHealing * damage) / damageToHeal;
                    ChangeDamage(type, -healing, ignoreDamageResistances, source, extraParams);

                    // remove this damage type from the damage we consider for future loops, regardless of how much we
                    // actually healed this type.
                    damageToHeal -= damage;
                    availableHealing -= healing;
                }
            }
            else if (amount > 0)
            {
                // We are adding damage. Keep track of how much we can dish out (with a better var name for readbility).
                var availableDamage = amount;

                // How many damage types do we have to distribute over?.
                var numberDamageTypes = types.Length;

                // Apply damage to each damage group
                int damage;
                foreach (var type in types)
                {
                    damage = availableDamage / numberDamageTypes;
                    availableDamage -= damage;
                    numberDamageTypes -= 1;

                    // Damage is applied. If damage type is not supported, this has no effect.
                    // This may results in less total damage change than naively expected, but is intentional.
                    ChangeDamage(type, damage, ignoreDamageResistances, source, extraParams);
                }
            }
        }

        public bool SetDamage(DamageTypePrototype type, int newValue, IEntity? source = null,  DamageChangeParams? extraParams = null)
        {
            if (!_damageDict.TryGetValue(type, out var oldValue))
            {
                return false;
            }

            // TODO QUESTION what is this if statement supposed to do?
            // Is TotalDamage supposed to be something like MaxDamage? I don't think DamageableComponents has a MaxDamage?
            if (newValue >= TotalDamage)
            {
                return true;
            }

            if (newValue < 0)
            {
                return true;
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

        // TODO QUESTION I created this function, and the one below it, to covert between Dictionary<IPrototype,int> and
        // Dictionary<string,int> using the IPrototype ID field. This sort of action is neededwhen sending damage
        // dictionary data over the network, as is apparently doesn't support sending prototypes. However, given how
        // generalizable this function is, and that it may be usefull when sending other prototype data, this function
        // should probably be moved somewhere else. Would this belong in PrototypeManager?
        // Or it might just become obsolete whenever that is supported.

        /// <summary>
        /// Take a dictionary with protoype keys, and return a dictionary using the prototype ID strings as keys
        /// instead.
        /// </summary>
        /// <remarks>
        /// Usefull when sending prototypes dictionaries over the network.
        /// </remarks>
        public static IReadOnlyDictionary<string, TValue>
            ConvertDictKeysToIDs<TPrototype,TValue>(IReadOnlyDictionary<TPrototype, TValue> prototypeDict)
            where TPrototype : IPrototype
        {
            Dictionary<string, TValue> idDict = new(prototypeDict.Count);
            foreach (var entry in prototypeDict)
            {
                idDict.Add(entry.Key.ID, entry.Value);
            }
            return idDict;
        }

        /// <summary>
        /// Takes a dictionary with strings as keys and attempts to return one using Prototypes as keys.
        /// </summary>
        /// <remarks>
        /// Finds prototypes with matching IDs using the prototype manager. Usefull when receiving prototypes
        /// dictionaries over the network.
        /// </remarks>
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

        // TODO QUESTION I thought Prototypes could not be sent over the network? Was that just wrong or is this
        // function doing something else? TBH I have no idea what its for.
        public DamageableComponentState(Dictionary<DamageTypePrototype, int> damageList) 

        {
            DamageList = damageList;
        }
    }
}
