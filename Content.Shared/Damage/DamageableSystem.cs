using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameObjects;
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
            SubscribeLocalEvent<DamageableComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DamageableComponent, TryChangeDamageEvent>(TryChangeDamage);
        }

        /// <summary>
        ///     Initialize a damageable component
        /// </summary>
        private void OnInit(EntityUid uid, DamageableComponent component, ComponentInit _)
        {
            // Get resistance set, if any was specified.
            if (component.ResistanceSetID != null)
            {
                _prototypeManager.TryIndex(component.ResistanceSetID, out component.ResistanceSet);
            }

            // Note that component.DamageContainerID may be null despite being a required data field. In particular,
            // this can happen when adding this component via ViewVariables.

            if (component.DamageContainerID == null ||
                !_prototypeManager.TryIndex<DamageContainerPrototype>(component.DamageContainerID, out var damageContainerPrototype) ||
                damageContainerPrototype.SupportAll )
            {
                // Either the container should support all damage types, or no valid DamageContainerID was given (for
                // which we default to support all). Add every single damage type to our component:
                foreach (var type in _prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
                {
                    component.DamagePerType.Add(type.ID, 0);
                }

                DamageChanged(uid, component, false);
                return;
            }

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

            DamageChanged(uid, component, false);
        }

        /// <summary>
        ///     If the damage in a DamageableComponent was changed, this function should be called.
        /// </summary>
        /// <remarks>
        ///     This updates cached damage information, flags the component as dirty, and raises a damage changed event.
        ///     The damage changed event is used by other systems, such as damage thresholds.
        /// </remarks>
        public void DamageChanged(EntityUid uid, DamageableComponent component, bool damageIncreased)
        {
            component.TotalDamage = component.DamagePerType.Values.Sum();
            component.DamagePerGroup = GetDamagePerGroup(component);
            component.Dirty();
            RaiseLocalEvent(uid, new DamageChangedEvent(component, damageIncreased), false);
        }

        /// <summary>
        ///     Applies damage to the component, using damage specified via a <see cref="DamageSpecifier"/> instance.
        /// </summary>
        /// <remarks>
        ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
        ///     function just applies the container's resistances (unless otherwise specified) and then changes the
        ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
        /// </remarks>
        /// <returns>
        ///     Returns false if a no damage change occurred; true otherwise.
        /// </returns>
        public void TryChangeDamage(EntityUid uid, DamageableComponent component, TryChangeDamageEvent args)
        {
            if (args.Damage == null)
            {
                // This should never happen. Damage data should be a required data field. However, the YAML linter
                // currently does not properly detect this. Logs are better than hard-crashes. Lets hope I didn't miss
                // too many YAML files.
                Logger.Error("Null DamageSpecifier. Probably because a required yaml field was not given.");
                return;
            }

            //Check that the DamageSpecifier actually contains data:
            if (args.Damage.DamageDict.Count() == 0)
            {
                // This can happen if AfterDeserialization hooks were not called, or if someone performed
                // math-operations before calling the hooks. An example of this would be when someone uses an abstract
                // entity, as these do not call deserialization hooks.
                Logger.Warning("Empty DamageSpecifier passed to DamageableComponent. Was AfterDeserialization not called?");
                return;
            }

            // Apply resistances
            var damage = args.Damage;
            if (!args.IgnoreResistances && component.ResistanceSet != null)
            {
                damage = DamageSpecifier.ApplyResistanceSet(damage, component.ResistanceSet);

                // Has the resistance set removed all damage?
                if (damage.TotalAbsoluteDamage() == 0) return;
            }

            // Deal/heal damage, while keeping track of whether the damage changed.
            // Also track whether any damage was dealt, or whether it was all healing.
            var damageIncreased = false;
            var damageChanged = false;
            foreach (var entry in damage.DamageDict)
            {
                if (entry.Value == 0) continue;

                // This is where we actually apply damage, using the TryChangeDamage() function
                if (!TryChangeDamage(component, entry.Key, entry.Value))
                    continue;

                damageChanged = true;
                if (entry.Value > 0) damageIncreased = true; 
            }

            // If any damage change occurred, update the other data on the damageable component and re-sync
            if (damageChanged)
            {
                DamageChanged(uid, component, damageIncreased);
            }
        }

        /// <summary>
        ///     Tries to change the specified <see cref="DamageTypePrototype"/>.
        /// </summary>
        /// <returns>
        ///     False if the given type is not supported or no damage change occurred; true otherwise.
        /// </returns>
        public static bool TryChangeDamage(DamageableComponent component, string damageType, int changeAmount)
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
            DamageChanged(component.Owner.Uid, component, damageIncreased: false);
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
        public Dictionary<string, int> GetDamagePerGroup(DamageableComponent component)
        {
            var damageGroupDict = new Dictionary<string, int>();
            foreach (var group in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
            {
                var groupDamage = 0;
                var groupIsSupported = false;

                foreach (var type in group.DamageTypes)
                {
                    if (component.DamagePerType.TryGetValue(type, out var damage))
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
    }

    public class DamageChangedEvent : EntityEventArgs
    {
        /// <summary>
        ///     This is the component whose damage .  
        /// </summary>
        /// <remarks>
        ///     Given that nearly every component that cares about a change in the damage, needs to know the
        ///     current damage values, directly passing this information prevents a lot of duplicate
        ///     Owner.TryGetComponent() calls. One of the few exceptions is lightbulbs, which just care if ANY damage
        ///     was taken, not how much.
        /// </remarks>
        public readonly DamageableComponent Damageable;

        /// <summary>
        ///     Has any damage type increased? 
        /// </summary>
        /// <remarks>
        ///     This can still be true even if the overall effect of the damage change was to reduce the total damage.
        /// </remarks>
        public readonly bool DamageIncreased;
        public DamageChangedEvent(DamageableComponent damageable, bool damageIncreased)
        {
            Damageable = damageable;
            DamageIncreased = damageIncreased;
        }
    }

    /// <summary>
    ///     Event used to deal or heal damage on a damageable component. Handled by <see cref="DamageableSystem"/>
    /// </summary>
    public class TryChangeDamageEvent : EntityEventArgs
    {
        /// <summary>
        ///     Damage that is added to the DamageableComponent
        /// </summary>
        public readonly DamageSpecifier Damage;

        /// <summary>
        ///     Whether to ignore resistances of the damageable component. Healing ignores resistances. Defaults
        ///     to false.
        /// </summary>
        public readonly bool IgnoreResistances;

        public TryChangeDamageEvent(DamageSpecifier damage, bool ignoreResistances = false)
        {
            Damage = damage;
            IgnoreResistances = ignoreResistances;
        }
    }
}
