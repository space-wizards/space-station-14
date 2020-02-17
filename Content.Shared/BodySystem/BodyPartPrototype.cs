using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;


namespace Content.Shared.BodySystem {


    /// <summary>
    ///    Prototype for the BodyPart class.
    /// </summary>	
    [Prototype("bodyPart")]
    [NetSerializable, Serializable]
    public class BodyPartPrototype : IPrototype, IIndexedPrototype {
        private string _id;
        private string _name;
		private string _plural;
		private BodyPartType _partType;
		private int _durability;
		private int _destroyThreshold;
		private float _resistance;
		private int _size;
		private BodyPartCompatibility _compatability;
		private List<IExposeData> _properties;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public string Name => _name;
		
        [ViewVariables]
        public string Plural => _plural;  
		
        [ViewVariables]
        public BodyPartType PartType => _partType;
		
		[ViewVariables]
		public int Durability => _durability;
		
		[ViewVariables]
		public int DestroyThreshold => _destroyThreshold;	
			
		[ViewVariables]
		public float Resistance => _resistance;
			
		[ViewVariables]
		public int Size => _size;
		
        [ViewVariables]
        public BodyPartCompatibility Compatability => _compatability;
		
        [ViewVariables]
        public List<IExposeData> Properties => _properties;

        public virtual void LoadFrom(YamlMappingNode mapping){
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _plural, "plural", string.Empty);
			serializer.DataField(ref _partType, "partType", BodyPartType.Other);
			serializer.DataField(ref _durability, "durability", 50);
			serializer.DataField(ref _destroyThreshold, "destroyThreshold", -50);
			serializer.DataField(ref _resistance, "resistance", 0f);
			serializer.DataField(ref _size, "size", 0);
			serializer.DataField(ref _compatability, "compatability", BodyPartCompatibility.Universal);
			serializer.DataField(ref _properties, "properties", new List<IExposeData>());
        }
    }
}
