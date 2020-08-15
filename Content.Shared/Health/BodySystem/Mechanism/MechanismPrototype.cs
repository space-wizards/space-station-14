using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Health.BodySystem.Mechanism
{

    /// <summary>
    ///    Prototype for the Mechanism class.
    /// </summary>
    [Prototype("mechanism")]
    [NetSerializable, Serializable]
    public class MechanismPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private string _description;
        private string _examineMessage;
        private string _spritePath;
        private string _rsiPath;
        private string _rsiState;
        private int _durability;
        private int _destroyThreshold;
        private int _resistance;
        private int _size;
        private BodyPartCompatibility _compatibility;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public string Name => _name;

        [ViewVariables]
        public string Description => _description;

        [ViewVariables]
        public string ExamineMessage => _examineMessage;

        [ViewVariables]
        public string RSIPath => _rsiPath;

        [ViewVariables]
        public string RSIState => _rsiState;

        [ViewVariables]
        public int Durability => _durability;

        [ViewVariables]
        public int DestroyThreshold => _destroyThreshold;

        [ViewVariables]
        public int Resistance => _resistance;

        [ViewVariables]
        public int Size => _size;

        [ViewVariables]
        public BodyPartCompatibility Compatibility => _compatibility;

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _description, "description", string.Empty);
            serializer.DataField(ref _examineMessage, "examineMessage", string.Empty);
            serializer.DataField(ref _rsiPath, "rsiPath", string.Empty);
            serializer.DataField(ref _rsiState, "rsiState", string.Empty);
            serializer.DataField(ref _durability, "durability", 0);
            serializer.DataField(ref _destroyThreshold, "destroyThreshold", 0);
            serializer.DataField(ref _resistance, "resistance", 0);
            serializer.DataField(ref _size, "size", 2);
            serializer.DataField(ref _compatibility, "compatibility", BodyPartCompatibility.Universal);
        }
    }
}

