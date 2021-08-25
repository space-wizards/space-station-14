using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A damage container which can be used to specify support for various damage types.
    /// </summary>
    /// <remarks>
    ///     This is effectively just a list of damage types that can be specified in YAML files using both damage types
    ///     and damage groups. Currently this is only used to specify what damage types a <see
    ///     cref="DamageableComponent"/> should support.
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
        ///     Determines whether this DamageContainerPrototype will support ALL damage types and groups. If true, ignore
        ///     all other options.
        /// </summary>
        [DataField("supportAll")] private bool _supportAll;

        [DataField("supportedGroups")] private HashSet<string> _supportedDamageGroupIDs = new();
        [DataField("supportedTypes")] private HashSet<string> _supportedDamageTypeIDs = new();

        private HashSet<DamageTypePrototype> _supportedDamageTypes = new();

        /// <summary>
        ///     Collection of damage types supported by this container.
        /// </summary>
        [ViewVariables] public IReadOnlyCollection<DamageTypePrototype> SupportedDamageTypes => _supportedDamageTypes;

        void ISerializationHooks.AfterDeserialization()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            if (_supportAll)
            {
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

        }
    }
}
