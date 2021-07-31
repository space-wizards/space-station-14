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
    ///     Prototype for the DamageContainer class.
    /// </summary>
    [Prototype("damageContainer")]
    [Serializable, NetSerializable]
    public class DamageContainerPrototype : IPrototype, ISerializationHooks
    {
        private IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        /// <summary>
        /// Should this damage container accept ALL damage types and groups?
        /// </summary>
        [DataField("supportAll")] private bool _supportAll;

        /// <summary>
        /// List of damage groups that this damge container should support. If a group is specified, all group mmember damge
        /// types are also supported.
        /// </summary>
        [DataField("supportedGroups")] private HashSet<string> _supportedDamageGroupIDs = new();

        /// <summary>
        /// List of damage types that this damage container should support. This also adds some damage groups to the
        /// list of supported damage groups, such that this container is properly affected when attacked by that group
        /// of damage.
        /// </summary>
        [DataField("supportedTypes")] private HashSet<string> _supportedDamageTypeIDs = new();

        private HashSet<DamageGroupPrototype> _applicableDamageGroups = new();
        private HashSet<DamageTypePrototype> _supportedDamageTypes = new();

        // TODO NET 5 IReadOnlySet

        /// <summary>
        /// Collection of damage groups that could affect this container.
        /// </summary>
        /// <remarks>
        /// This describes what damage groups could have an effect on this damage container. However not every damage
        /// group has to be fully supported. For example, the container may support ONLY the piercing damage type. It should
        /// therefore be affected by instances of brute damage, but does not neccesarily support blunt or slash damage.
        /// For a list of supported damage types, see <see cref="SupportedDamageTypes"/>.
        /// </remarks>
        [ViewVariables] public IReadOnlyCollection<DamageGroupPrototype> ApplicableDamageGroups => _applicableDamageGroups;

        /// <summary>
        /// Collection of damage types supported by this container.
        /// </summary>
        /// <remarks>
        /// Each of these damage types is fully supported by the DamageContainer. If any of these damage types is a
        /// member of a damage group, these groups are added to <see cref="ApplicableDamageGroups"></see>
        /// </remarks>
        [ViewVariables] public IReadOnlyCollection<DamageTypePrototype> SupportedDamageTypes => _supportedDamageTypes;

        void ISerializationHooks.AfterDeserialization()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            // If all damge types are supported, add all of them.
            if (_supportAll)
            {
                foreach (var group in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    _applicableDamageGroups.Add(group);
                    foreach (var type in group.DamageTypes)
                    {
                        _supportedDamageTypes.Add(type);
                    }
                }

                return;
            }

            // Add fully supported damage groups
            foreach (var groupID in _supportedDamageGroupIDs)
            {
                var group = _prototypeManager.Index<DamageGroupPrototype>(groupID);
                _applicableDamageGroups.Add(group);
                foreach (var type in group.DamageTypes)
                {
                    _supportedDamageTypes.Add(type);
                }
            }

            // Add individual damage types
            foreach (var supportedTypeID in _supportedDamageTypeIDs)
            {
                var type = _prototypeManager.Index<DamageTypePrototype>(supportedTypeID);
                _supportedDamageTypes.Add(type);

                //Add any damge groups this damage type is a member of to _applicableDamageGroups
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
