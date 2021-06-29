using System;
using System.Collections.Generic;
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
<<<<<<< refs/remotes/origin/master
=======
        private IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

>>>>>>> update damagecomponent across shared and server
        [DataField("supportAll")] private bool _supportAll;
        [DataField("supportedClasses")] private HashSet<DamageClass> _supportedClasses = new();
        [DataField("supportedTypes")] private HashSet<DamageType> _supportedTypes = new();

        // TODO NET 5 IReadOnlySet
<<<<<<< refs/remotes/origin/master
        [ViewVariables] public IReadOnlyCollection<DamageClass> SupportedClasses => _supportedClasses;

        [ViewVariables] public IReadOnlyCollection<DamageType> SupportedTypes => _supportedTypes;
=======
        [ViewVariables] public IReadOnlyCollection<DamageGroupPrototype> SupportedDamageGroups => _supportedDamageGroups;
        [ViewVariables] public IReadOnlyCollection<DamageTypePrototype> SupportedDamageTypes => _supportedDamageTypes;
>>>>>>> update damagecomponent across shared and server

        void ISerializationHooks.AfterDeserialization()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            if (_supportAll)
            {
<<<<<<< refs/remotes/origin/master
                _supportedClasses.UnionWith(Enum.GetValues<DamageClass>());
                _supportedTypes.UnionWith(Enum.GetValues<DamageType>());
=======
                foreach (var DamageGroup in _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    _supportedDamageGroups.Add(DamageGroup);
                    foreach (var SupportedDamageType in DamageGroup.DamageTypes)
                    {
                        _supportedDamageTypes.Add(SupportedDamageType);
                    }
                }

>>>>>>> fix a few bugs
                return;
            }

            foreach (var supportedClass in _supportedClasses)
            {
<<<<<<< refs/remotes/origin/master
                foreach (var supportedType in supportedClass.ToTypes())
                {
                    _supportedTypes.Add(supportedType);
                }
            }

            foreach (var originalType in _supportedTypes)
            {
                _supportedClasses.Add(originalType.ToClass());
            }
=======
                var DamageGroup= _prototypeManager.Index<DamageGroupPrototype>(supportedClassID);
                _supportedDamageGroups.Add(DamageGroup);
                foreach (var DamageType in DamageGroup.DamageTypes)
                {
                    _supportedDamageTypes.Add(DamageType);
                }
            }

>>>>>>> fix a few bugs
        }
    }
}
