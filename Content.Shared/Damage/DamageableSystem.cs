using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage
{
    public class DamageableSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<DamageableComponent, ComponentInit>(DamageableInit);
            SubscribeLocalEvent<DamageableComponent, ComponentHandleState>(DamageableHandleState);
            SubscribeLocalEvent<DamageableComponent, ComponentGetState>(DamageableGetState);
        }

        /// <summary>
        ///     Initialize a damageable component
        /// </summary>
        private void DamageableInit(EntityUid uid, DamageableComponent component, ComponentInit _)
        {
            // Note that component.DamageContainerID may be null despite being a required data field. In particular,
            // this can happen when adding this component via ViewVariables.

            if (component.DamageContainerID == null ||
                !_prototypeManager.TryIndex<DamageContainerPrototype>(component.DamageContainerID, out var damageContainerPrototype) ||
                damageContainerPrototype.SupportAll)
            {
                // Either the container should support all damage types, or no valid DamageContainerID was given (for
                // which we default to support all). Add every single damage type to our component:
                foreach (var type in _prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
                {
                    component.DamagePerType.Add(type.ID, 0);
                }
            }
            else
            {
                // A valid damage container ID was provided. Initialize damage dictionary, using the types from the damage
                // container prototype
                component.DamagePerType = new(damageContainerPrototype.SupportedTypes.Count);
                foreach (var type in damageContainerPrototype.SupportedTypes)
                {
                    component.DamagePerType.Add(type, 0);
                }

                // Then also add the supported damage groups
                foreach (var groupID in damageContainerPrototype.SupportedGroups)
                {
                    var group = _prototypeManager.Index<DamageGroupPrototype>(groupID);
                    foreach (var type in group.DamageTypes)
                    {
                        component.DamagePerType.TryAdd(type, 0);
                    }
                }
            }

            // Initialize damage per group dictionary.
            component.DamagePerGroup = new Dictionary<string, int>();
            foreach (var group in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
            {
                foreach (var type in group.DamageTypes)
                {
                    if (component.DamagePerType.ContainsKey(type))
                    {
                        component.DamagePerGroup.TryAdd(group.ID, 0);
                        break;
                    }
                }
            }

            component.TotalDamage = 0;
        }

        /// <summary>
        ///     If the damage in a DamageableComponent was changed, this function should be called.
        /// </summary>
        /// <remarks>
        ///     This updates cached damage information, flags the component as dirty, and raises a damage changed event.
        ///     The damage changed event is used by other systems, such as damage thresholds.
        /// </remarks>
        public void DamageChanged(DamageableComponent component, bool damageIncreased)
        {
            component.TotalDamage = component.DamagePerType.Values.Sum();
            component.DamagePerGroup = GetDamagePerGroup(component.DamagePerType);
            component.Dirty();
            RaiseLocalEvent(component.Owner.Uid, new DamageChangedEvent(component, damageIncreased), false);
        }

        /// <summary>
        ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
        /// </summary>
        /// <remarks>
        ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
        ///     function just applies the container's resistances (unless otherwise specified) and then changes the
        ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
        /// </remarks>
        /// <returns>
        ///     Returns false if a no damage change occurred; true otherwise.
        /// </returns>
        public bool TryChangeDamage(EntityUid uid, DamageSpecifier damage, bool ignoreResistances = false)
        {
            if (!ComponentManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
            {
                // TODO BODY SYSTEM pass damage onto body system
                return false;
            }

            if (damage == null)
            {
                Logger.Error("Null DamageSpecifier. Probably because a required yaml field was not given.");
                return false;
            }

            //Check that the DamageSpecifier actually contains data:
            if (damage.DamageDict.Count() == 0)
            {
                Logger.Warning("Empty DamageSpecifier passed to DamageableComponent. Was AfterDeserialization not called?");
                return false;
            }

            // Apply resistances
            if (!ignoreResistances && damageable.ResistanceSetID != null)
            {
                if (_prototypeManager.TryIndex<ResistanceSetPrototype>(damageable.ResistanceSetID, out var resistanceSet))
                {
                    damage = DamageSpecifier.ApplyResistanceSet(damage, resistanceSet);
                }

                // Has the resistance set removed all damage?
                if (damage.TotalAbsoluteDamage() == 0)
                    return false;
            }

            // Deal/heal damage, while keeping track of whether the damage changed.
            // Also track whether any damage was dealt, or whether it was all healing.
            var damageIncreased = false;
            var damageChanged = false;
            foreach (var entry in damage.DamageDict)
            {
                if (entry.Value == 0) continue;

                // This is where we actually apply damage, using the TryChangeDamage() function
                if (!TryChangeDamage(damageable, entry.Key, entry.Value))
                    continue;

                damageChanged = true;
                if (entry.Value > 0) damageIncreased = true;
            }

            // If any damage change occurred, update the other data on the damageable component and re-sync
            if (damageChanged)
            {
                DamageChanged(damageable, damageIncreased);
            }

            return true;
        }

        /// <summary>
        ///     Tries to change the specified <see cref="DamageTypePrototype"/>.
        /// </summary>
        /// <returns>
        ///     False if the given type is not supported or no damage change occurred; true otherwise.
        /// </returns>
        public bool TryChangeDamage(DamageableComponent component, string damageType, int changeAmount)
        {
            // Check if damage type is supported, and get the current value if it is.
            if (!component.DamagePerType.TryGetValue(damageType, out var currentDamage))
            {
                return false;
            }

            // Are we healing below zero?
            if (currentDamage + changeAmount < 0)
            {
                if (currentDamage == 0)
                {
                    // Damage type is supported, but there is nothing to do.
                    return false;
                }

                // Cannot heal below zero
                changeAmount = -currentDamage;
            }

            component.DamagePerType[damageType] = currentDamage + changeAmount;
            return true;
        }

        /// <summary>
        ///     Sets all damage types supported by a <see cref="DamageableComponent"/> to the specified value.
        /// </summary>
        /// <remakrs>
        ///     Does nothing If the given damage value is negative.
        /// </remakrs>
        public void SetAllDamage(DamageableComponent component, int newValue)
        {
            if (newValue < 0)
            {
                // invalid value
                return;
            }

            foreach (var type in component.DamagePerType.Keys)
            {
                component.DamagePerType[type] = newValue;
            }

            // Setting damage does not count as 'dealing' damage, even if it is set to a larger value. Hence damageIncreased: false
            DamageChanged(component, damageIncreased: false);
        }

        /// <summary>
        ///     Given a dictionary with <see cref="DamageTypePrototype.ID"/> keys, convert it to a read-only dictionary
        ///     with <see cref="DamageGroupPrototype.ID"/> keys.
        /// </summary>
        /// <remarks>
        ///     Returns a dictionary with damage group keys, with values calculated by adding up the values for each
        ///     damage type in that group. If a damage type is associated with more than one supported damage group, it
        ///     will contribute to the total of each group. If a group has no supported damage types, it is not in the
        ///     resulting dictionary.
        /// </remarks>
        public Dictionary<string, int> GetDamagePerGroup(Dictionary<string, int> damagePerType)
        {
            var damageGroupDict = new Dictionary<string, int>();
            foreach (var group in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
            {
                var groupDamage = 0;
                var groupIsSupported = false;

                foreach (var type in group.DamageTypes)
                {
                    if (damagePerType.TryGetValue(type, out var damage))
                    {
                        groupIsSupported = true;
                        groupDamage += damage;
                    }
                }

                // was at least one member of this group actually supported by the container?
                if (groupIsSupported)
                {
                    damageGroupDict.Add(group.ID, groupDamage);
                }
            }
            return damageGroupDict;
        }

        private void DamageableGetState(EntityUid uid, DamageableComponent component, ref ComponentGetState args)
        {
            args.State = new DamageableComponentState(component.DamagePerType, component.ResistanceSetID);
        }

        private void DamageableHandleState(EntityUid uid, DamageableComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not DamageableComponentState state)
            {
                return;
            }

            component.ResistanceSetID = state.ResistanceSetID;

            // Check if the damage per type has updated. Usually a damage change is apparent by just looking at the
            // total. Only check every single damage type if we need to.
            var newTotalDamage = state.DamagePerType.Values.Sum();
            if (component.TotalDamage == newTotalDamage &&
                component.DamagePerType.Count == state.DamagePerType.Count)
            {
                // Check every damage type for changes
                var damageChanged = false;
                foreach (var (type, newValue) in state.DamagePerType)
                {
                    if (!component.DamagePerType.TryGetValue(type, out var oldValue) ||
                        oldValue != newValue)
                    {
                        damageChanged = true;
                        break;
                    }
                }

                if (!damageChanged)
                    return;
            }

            component.DamagePerType = state.DamagePerType;

            // Calculate dependent values and raise a local event. The event is needed as there may be client-exclusive
            // systems (e.g. UI) that need to know if damage changed as a result of server-exclusive damage-dealing
            // systems.
            DamageChanged(component, component.TotalDamage < newTotalDamage);
        }
    }

    public class DamageChangedEvent : EntityEventArgs
    {
        /// <summary>
        ///     This is the component whose damage was changed.  
        /// </summary>
        /// <remarks>
        ///     Given that nearly every component that cares about a change in the damage, needs to know the
        ///     current damage values, directly passing this information prevents a lot of duplicate
        ///     Owner.TryGetComponent() calls.
        /// </remarks>
        public readonly DamageableComponent Damageable;

        /// <summary>
        ///     Has the total damage in the container increased?
        /// </summary>
        public readonly bool DamageIncreased;
        public DamageChangedEvent(DamageableComponent damageable, bool damageIncreased)
        {
            Damageable = damageable;
            DamageIncreased = damageIncreased;
        }
    }
}
