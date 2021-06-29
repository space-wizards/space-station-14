#nullable enable
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
        [DataField("supportedClasses")] private HashSet<string> _supportedDamageGroupsButAsStrings = new();
        [DataField("supportedTypes")] private HashSet<string> _supportedDamageTypesButAsStrings = new();

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
                foreach (var DamageGroup in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    _supportedDamageGroups.Add(DamageGroup);
                    foreach (var SupportedDamageType in DamageGroup.DamageTypes)
                    {
                        _supportedDamageTypes.Add(SupportedDamageType);
                    }
                }

                return;
            }

            foreach (var supportedClassID in _supportedDamageGroupsButAsStrings)
            {
                var DamageGroup= _prototypeManager.Index<DamageGroupPrototype>(supportedClassID);
                _supportedDamageGroups.Add(DamageGroup);
                foreach (var DamageType in DamageGroup.DamageTypes)
                {
                    _supportedDamageTypes.Add(DamageType);
                }
            }

        }
    }
}
