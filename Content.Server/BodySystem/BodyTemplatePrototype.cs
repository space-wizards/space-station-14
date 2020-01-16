    
	
	[Prototype("bodyTemplate")]
    public class BodyTemplate : IPrototype, IIndexedPrototype {
        private string _id;
        private string _name;
		private string _centerSlot;
		private Dictionary<BodyPartType, string> _slots;
		private Dictionary<string, string> _connections;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public string Name => _name;
		
        [ViewVariables]
        public string CenterSlot => _centerSlot;
		
        [ViewVariables]
        public Dictionary<BodyPartType, string> Slots;
		
        [ViewVariables]
        public Dictionary<string, string> Connections;



        public virtual void LoadFrom(YamlMappingNode mapping){
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
			serializer.DataField(ref _centerSlot "centerSlot", string.Empty);
            serializer.DataField(ref _slots, "slots", new Dictionary<BodyPartType, string>());
			serializer.DataField(ref _connections, "connections", new Dictionary<string, string>());
        }
    }
		  