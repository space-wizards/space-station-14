using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Damage.DamageContainer
{
    /// <summary>
    ///     Prototype for the DamageContainer class.
    /// </summary>
    [Prototype("damageContainer")]
    [Serializable, NetSerializable]
    public class DamageContainerPrototype : IPrototype, IIndexedPrototype
    {
        private List<DamageClass> _activeDamageClasses;
        private string _id;

        [ViewVariables] public List<DamageClass> ActiveDamageClasses => _activeDamageClasses;

        [ViewVariables] public string ID => _id;

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _activeDamageClasses, "activeDamageClasses", new List<DamageClass>());
        }
    }
}
