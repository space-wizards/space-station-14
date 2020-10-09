using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Body.Part
{
    /// <summary>
    ///     Prototype for the BodyPart class.
    /// </summary>
    [Prototype("bodyPart")]
    [Serializable, NetSerializable]
    public class BodyPartPrototype : IPrototype, IIndexedPrototype
    {
        private BodyPartCompatibility _compatibility;
        private string _damageContainerPresetId;
        private int _destroyThreshold;
        private int _durability;
        private string _id;
        private List<string> _mechanisms;
        private string _name;
        private BodyPartType _partType;
        private string _plural;
        private List<IExposeData> _properties;
        private float _resistance;
        private string _resistanceSetId;
        private string _rsiPath;
        private string _rsiState;
        private int _size;
        private string _surgeryDataName;
        private bool _isVital;


        [ViewVariables] public string Name => _name;

        [ViewVariables] public string Plural => _plural;

        [ViewVariables] public string RSIPath => _rsiPath;

        [ViewVariables] public string RSIState => _rsiState;

        [ViewVariables] public BodyPartType PartType => _partType;

        [ViewVariables] public int Durability => _durability;

        [ViewVariables] public int DestroyThreshold => _destroyThreshold;

        [ViewVariables] public float Resistance => _resistance;

        [ViewVariables] public int Size => _size;

        [ViewVariables] public BodyPartCompatibility Compatibility => _compatibility;

        [ViewVariables] public string DamageContainerPresetId => _damageContainerPresetId;

        [ViewVariables] public string ResistanceSetId => _resistanceSetId;

        [ViewVariables] public string SurgeryDataName => _surgeryDataName;

        [ViewVariables] public List<IExposeData> Properties => _properties;

        [ViewVariables] public List<string> Mechanisms => _mechanisms;

        [ViewVariables] public string ID => _id;

        [ViewVariables] public bool IsVital => _isVital;

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _plural, "plural", string.Empty);
            serializer.DataField(ref _rsiPath, "rsiPath", string.Empty);
            serializer.DataField(ref _rsiState, "rsiState", string.Empty);
            serializer.DataField(ref _partType, "partType", BodyPartType.Other);
            serializer.DataField(ref _surgeryDataName, "surgeryDataType", "BiologicalSurgeryData");
            serializer.DataField(ref _durability, "durability", 50);
            serializer.DataField(ref _destroyThreshold, "destroyThreshold", -50);
            serializer.DataField(ref _resistance, "resistance", 0f);
            serializer.DataField(ref _size, "size", 0);
            serializer.DataField(ref _compatibility, "compatibility", BodyPartCompatibility.Universal);
            serializer.DataField(ref _damageContainerPresetId, "damageContainer", string.Empty);
            serializer.DataField(ref _resistanceSetId, "resistances", string.Empty);
            serializer.DataField(ref _properties, "properties", new List<IExposeData>());
            serializer.DataField(ref _mechanisms, "mechanisms", new List<string>());
            serializer.DataField(ref _isVital, "isVital", false);

            foreach (var property in _properties)
            {
                if (_properties.Count(x => x.GetType() == property.GetType()) > 1)
                {
                    throw new InvalidOperationException(
                        $"Multiple {nameof(BodyPartPrototype)} of the same type were defined in prototype {ID}");
                }
            }
        }
    }
}
