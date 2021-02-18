using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.DamageContainer
{
    /// <summary>
    ///     Prototype for the DamageContainer class.
    /// </summary>
    [Prototype("damageContainer")]
    [Serializable, NetSerializable]
    public class DamageContainerPrototype : IPrototype, IIndexedPrototype, ISerializationHooks
    {
        [DataField("supportAll")] private bool _supportAll;
        [DataField("supportedClasses")] private HashSet<DamageClass> _supportedClasses;
        [DataField("supportedTypes")] private HashSet<DamageType> _supportedTypes;
        [DataField("id")] private string _id;

        // TODO NET 5 IReadOnlySet
        [ViewVariables] public IReadOnlyCollection<DamageClass> SupportedClasses => _supportedClasses;

        [ViewVariables] public IReadOnlyCollection<DamageType> SupportedTypes => _supportedTypes;

        [ViewVariables] public string ID => _id;

        public void AfterDeserialization()
        {
            if (_supportAll)
            {
                _supportedClasses.UnionWith(Enum.GetValues<DamageClass>());
                _supportedTypes.UnionWith(Enum.GetValues<DamageType>());
                return;
            }

            foreach (var supportedClass in _supportedClasses)
            {
                foreach (var supportedType in supportedClass.ToTypes())
                {
                    _supportedTypes.Add(supportedType);
                }
            }

            foreach (var originalType in _supportedTypes)
            {
                _supportedClasses.Add(originalType.ToClass());
            }
        }
    }
}
