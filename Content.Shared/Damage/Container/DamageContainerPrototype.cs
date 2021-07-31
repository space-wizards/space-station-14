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

        [DataField("supportAll")] private bool _supportAll;
        [DataField("supportedGroups")] private HashSet<string> _supportedDamageGroupIDs = new();
        [DataField("supportedTypes")] private HashSet<string> _supportedDamageTypeIDs = new();

        private HashSet<DamageGroupPrototype> _supportedDamageGroups = new();
        private HashSet<DamageTypePrototype> _supportedDamageTypes = new();

        // TODO NET 5 IReadOnlySet
        [ViewVariables] public IReadOnlyCollection<DamageGroupPrototype> SupportedDamageGroups => _supportedDamageGroups;
        [ViewVariables] public IReadOnlyCollection<DamageTypePrototype> SupportedDamageTypes => _supportedDamageTypes;

        void ISerializationHooks.AfterDeserialization()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            if (_supportAll)
            {
                foreach (var group in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    _supportedDamageGroups.Add(group);
                    foreach (var type in group.DamageTypes)
                    {
                        _supportedDamageTypes.Add(type);
                    }
                }

                return;
            }

            foreach (var groupID in _supportedDamageGroupIDs)
            {
                var group = _prototypeManager.Index<DamageGroupPrototype>(groupID);
                _supportedDamageGroups.Add(group);
                foreach (var type in group.DamageTypes)
                {
                    _supportedDamageTypes.Add(type);
                }
            }

            foreach (var supportedTypeID in _supportedDamageTypeIDs)
            {
                _supportedDamageTypes.Add(_prototypeManager.Index<DamageTypePrototype>(supportedTypeID));
            }

        }
    }
}
