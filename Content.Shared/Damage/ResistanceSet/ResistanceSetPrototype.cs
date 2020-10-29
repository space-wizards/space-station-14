using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Damage.ResistanceSet
{
    /// <summary>
    ///     Prototype for the BodyPart class.
    /// </summary>
    [Prototype("resistanceSet")]
    [Serializable, NetSerializable]
    public class ResistanceSetPrototype : IPrototype, IIndexedPrototype
    {
        private Dictionary<DamageType, float> _coefficients;
        private Dictionary<DamageType, int> _flatReductions;
        private string _id;

        [ViewVariables] public Dictionary<DamageType, float> Coefficients => _coefficients;

        [ViewVariables] public Dictionary<DamageType, int> FlatReductions => _flatReductions;

        [ViewVariables] public Dictionary<DamageType, ResistanceSetSettings> Resistances { get; private set; }

        [ViewVariables] public string ID => _id;

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _coefficients, "coefficients", null);
            serializer.DataField(ref _flatReductions, "flatReductions", null);

            Resistances = new Dictionary<DamageType, ResistanceSetSettings>();
            foreach (var damageType in (DamageType[]) Enum.GetValues(typeof(DamageType)))
            {
                Resistances.Add(damageType,
                    new ResistanceSetSettings(_coefficients[damageType], _flatReductions[damageType]));
            }
        }
    }
}
