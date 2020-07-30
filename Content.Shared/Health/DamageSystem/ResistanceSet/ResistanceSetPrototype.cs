using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.DamageSystem
{
    /// <summary>
    ///    Prototype for the BodyPart class.
    /// </summary>	
    [Prototype("resistanceSet")]
    [NetSerializable, Serializable]
    public class ResistanceSetPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private Dictionary<DamageType, float> _coefficients;
        private Dictionary<DamageType, int> _flatReductions;
        private Dictionary<DamageType, ResistanceSetSettings> _resistances;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public Dictionary<DamageType, float> Coefficients => _coefficients;

        [ViewVariables]
        public Dictionary<DamageType, int> FlatReductions => _flatReductions;

        [ViewVariables]
        public Dictionary<DamageType, ResistanceSetSettings> Resistances => _resistances;

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _coefficients, "coefficients", null);
            serializer.DataField(ref _flatReductions, "flatReductions", null);

            _resistances = new Dictionary<DamageType, ResistanceSetSettings>();
            foreach (DamageType damageType in (DamageType[]) Enum.GetValues(typeof(DamageType)))
            {
                _resistances.Add(damageType, new ResistanceSetSettings(_coefficients[damageType], _flatReductions[damageType]));
            }
        }
    }
}
