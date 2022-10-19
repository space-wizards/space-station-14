using Content.Shared.Materials;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Prototypes
{
    [NetSerializable, Serializable, Prototype("latheRecipe")]
    public readonly record struct LatheRecipePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("name")] private readonly string _name = string.Empty;

        [DataField("icon")] private readonly SpriteSpecifier _icon = SpriteSpecifier.Invalid;

        [DataField("description")] private readonly string _description = string.Empty;

        [DataField("result", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private readonly string _result = string.Empty;

        [DataField("completetime")] private readonly TimeSpan _completeTime = TimeSpan.FromSeconds(5);

        [DataField("materials", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, MaterialPrototype>))]
        private readonly Dictionary<string, int> _requiredMaterials = new();

        /// <summary>
        ///     Name displayed in the lathe GUI.
        /// </summary>
        [ViewVariables]
        public string Name
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_name))
                    return _name;

                return !IoCManager.Resolve<IPrototypeManager>().TryIndex<EntityPrototype>(_result, out var prototype)
                    ? _name
                    : prototype.Value.Name;
            }
        }

        /// <summary>
        ///     Short description displayed in the lathe GUI.
        /// </summary>
        [ViewVariables]
        public string Description
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_description))
                    return _description;

                return !IoCManager.Resolve<IPrototypeManager>().TryIndex<EntityPrototype>(_result, out var prototype)
                    ? _description
                    : prototype.Value.Description;
            }
        }

        /// <summary>
        ///     Texture path used in the lathe GUI.
        /// </summary>
        [ViewVariables]
        public SpriteSpecifier Icon => _icon;

        /// <summary>
        ///     The prototype name of the resulting entity when the recipe is printed.
        /// </summary>
        [ViewVariables]
        public string Result => _result;

        /// <summary>
        ///     The materials required to produce this recipe.
        ///     Takes a material ID as string.
        /// </summary>
        [ViewVariables]
        public IReadOnlyDictionary<string, int> RequiredMaterials => _requiredMaterials;


        /// <summary>
        ///     How many milliseconds it'll take for the lathe to finish this recipe.
        ///     Might lower depending on the lathe's upgrade level.
        /// </summary>
        [ViewVariables]
        public TimeSpan CompleteTime => _completeTime;
    }
}
