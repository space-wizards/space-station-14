


    [Prototype("bodyPart")]
    public partial class BodyPart : IPrototype, IIndexedPrototype {
        private string _id;
        private string _name;
		private string _plural;
		private BodyPartType _type;
		private int _durability;
		private float _currentDurability;
		private int _destroyThreshold;
		private float _resistance;
		private int _size;
		private BodyPartCompatability _compatability;
		private List<IExposeData> _properties;

        [ViewVariables]
        public string ID => _id;

        /// <summary>
        ///     Body part name.
        /// </summary>
        [ViewVariables]
        public string Name => _name;
		
         /// <summary>
        ///     Plural version of this body part's name.
        /// </summary>
        [ViewVariables]
        public string Plural => _plural;  
		
         /// <summary>
        ///     Plural version of this body part's name.
        /// </summary>
        [ViewVariables]
        public string Type => _type;
		
        /// <summary>
        ///     Max HP of this body part.
        /// </summary>		
		[ViewVariables]
		public int Durability => _durability;
		
        /// <summary>
        ///     Current HP of this body part.
        /// </summary>		
		[ViewVariables]
		public int CurrentDurability => _currentDurability;
		
		/// <summary>
        ///     At what HP this body part is completely destroyed.
        /// </summary>		
		[ViewVariables]
		public int DestroyThreshold => _destroyThreshold;	
		
        /// <summary>
        ///     Armor of the body part against attacks.
        /// </summary>		
		[ViewVariables]
		public int Resistance => _resistance;
		
        /// <summary>
        ///     Determines many things: how many mechanisms can be fit inside a body part, fitting through tiny crevices, etc.
        /// </summary>		
		[ViewVariables]
		public int Size => _size;
		
        /// <summary>
        ///     What types of body parts this body part can attach to. For the most part, the only universal body parts should be bionic interfaces.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatability Compatability => _compatability;
		
        /// <summary>
        ///     List of IExposeData properties, allowing for unique properties to be attached to a limb.
        /// </summary>
        [ViewVariables]
        public IReadOnlyList<IExposeData> Properties => _properties;


        public virtual void LoadFrom(YamlMappingNode mapping){
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _plural, "plural", string.Empty);
			serializer.DataField(ref _type, "type", BodyPartType.other);
			serializer.DataField(ref _durability, "durability", 50);
			_currentDurability = (float)_durability;
			serializer.DataField(ref _destroyThreshold, "destroyThreshold", -50);
			serializer.DataField(ref _resistance, "resistance", 0f);
			serializer.DataField(ref _size, "size", 0);
			serializer.DataField(ref _compatability, "compatability", BodyPartCompatability.universal);
			serializer.DataField(ref _properties, "properties", new List<IExposeData>());
        }
    }