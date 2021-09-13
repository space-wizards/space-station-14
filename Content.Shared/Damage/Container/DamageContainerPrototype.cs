using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.Container
{
    /// <summary>
    ///     A damage container which can be used to specify support for various damage types.
    /// </summary>
    /// <remarks>
    ///     This is effectively just a list of damage types that can be specified in YAML files using both damage types
    ///     and damage groups. Currently this is only used to specify what damage types a <see
    ///     cref="Components.DamageableComponent"/> should support.
    /// </remarks>
    [Prototype("damageContainer")]
    [Serializable, NetSerializable]
    public class DamageContainerPrototype : IPrototype, ISerializationHooks
    {
        private IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        /// <summary>
        ///     Determines whether this DamageContainerPrototype will support ALL damage types and groups. If true,
        ///     ignore all other options.
        /// </summary>
        [DataField("supportAll")] private bool _supportAll;

        [DataField("supportedGroups")] private HashSet<string> _supportedDamageGroupIDs = new();
        [DataField("supportedTypes")] private HashSet<string> _supportedDamageTypeIDs = new();

        private HashSet<DamageGroupPrototype> _applicableDamageGroups = new();
        private HashSet<DamageGroupPrototype> _fullySupportedDamageGroups = new();
        private HashSet<DamageTypePrototype> _supportedDamageTypes = new();

        // TODO NET 5 IReadOnlySet

        /// <summary>
        ///     Collection of damage groups that can affect this container.
        /// </summary>
        /// <remarks>
        ///     This describes what damage groups can have an effect on this damage container. However not every damage
        ///     group has to be fully supported. For example, the container may support ONLY the piercing damage type.
        ///     It should therefore be affected by instances of brute group damage, but does not necessarily support
        ///     blunt or slash damage. If damage containers are only specified by supported damage groups, and every
        ///     damage type is in only one damage group, then SupportedDamageTypes should be equal to
        ///     ApplicableDamageGroups. For a list of supported damage types, see <see cref="SupportedDamageTypes"/>.
        /// </remarks>
        [ViewVariables] public IReadOnlyCollection<DamageGroupPrototype> ApplicableDamageGroups => _applicableDamageGroups;

        /// <summary>
        ///     Collection of damage groups that are fully supported by this container.
        /// </summary>
        /// <remarks>
        ///     This describes what damage groups this damage container explicitly supports. It supports every damage
        ///     type contained in these damage groups. It may also support other damage types not in these groups. To
        ///     see all damage types <see cref="SupportedDamageTypes"/>, and to see all applicable damage groups <see
        ///     cref="ApplicableDamageGroups"/>.
        /// </remarks>
        [ViewVariables] public IReadOnlyCollection<DamageGroupPrototype> FullySupportedDamageGroups => _fullySupportedDamageGroups;

        /// <summary>
        ///     Collection of damage types supported by this container.
        /// </summary>
        /// <remarks>
        ///     Each of these damage types is fully supported by the DamageContainer. If any of these damage types is a
        ///     member of a damage group, these groups are added to <see cref="ApplicableDamageGroups"></see>
        /// </remarks>
        [ViewVariables] public IReadOnlyCollection<DamageTypePrototype> SupportedDamageTypes => _supportedDamageTypes;

        void ISerializationHooks.AfterDeserialization()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            if (_supportAll)
            {
                foreach (var group in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    _applicableDamageGroups.Add(group);
                    _fullySupportedDamageGroups.Add(group);
                }
                foreach (var type in _prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
                {
                    _supportedDamageTypes.Add(type);
                }
                return;
            }

            // Add fully supported damage groups
            foreach (var groupID in _supportedDamageGroupIDs)
            {
                var group = _prototypeManager.Index<DamageGroupPrototype>(groupID);
                _fullySupportedDamageGroups.Add(group);
                foreach (var type in group.DamageTypes)
                {
                    _supportedDamageTypes.Add(type);
                }
            }

            // Add individual damage types, that are either not part of a group, or whose groups are (possibly) not fully supported
            foreach (var supportedTypeID in _supportedDamageTypeIDs)
            {
                var type = _prototypeManager.Index<DamageTypePrototype>(supportedTypeID);
                _supportedDamageTypes.Add(type);
            }

            // For whatever reason, someone may have listed all members of a group as supported instead of just listing
            // the group as supported. Check for this.
            foreach (var group in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
            {
                if (_fullySupportedDamageGroups.Contains(group))
                {
                    continue;
                }
                // The group is not in the list of fully supported groups. Should it be?
                var allMembersSupported = true;
                foreach (var type in group.DamageTypes)
                {
                    if (!_supportedDamageTypes.Contains(type))
                    {
                        // not all members are supported
                        allMembersSupported = false;
                        break;
                    }
                }
                if (allMembersSupported) {
                    // All members are supported. The silly goose should have just used a damage group.
                    _fullySupportedDamageGroups.Add(group);
                } 
            }

            // For each supported damage type, check whether it is in any existing group, If it is add it to _applicableDamageGroups
            foreach (var type in _supportedDamageTypes)
            {
                foreach (var group in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    if (group.DamageTypes.Contains(type))
                    {
                        _applicableDamageGroups.Add(group);
                    }
                }
            }
        }
    }
}
