using Content.Shared.Materials;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Prototypes
{
    [NetSerializable, Serializable, Prototype("latheRecipe")]
    public sealed class LatheRecipePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("name")]
        private string _name = string.Empty;

        [DataField("icon")]
        private SpriteSpecifier _icon = SpriteSpecifier.Invalid;

        [DataField("description")]
        private string _description = string.Empty;

        [DataField("result", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string _result = string.Empty;

        [DataField("completetime")]
        private TimeSpan _completeTime = TimeSpan.FromSeconds(5);

        [DataField("materials", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, MaterialPrototype>))]
        private Dictionary<string, int> _requiredMaterials = new();

        /// <summary>
        ///     Name displayed in the lathe GUI.
        /// </summary>
        [ViewVariables]
        public string Name
        {
            get
            {
                if (_name.Trim().Length != 0) return _name;
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                protoMan.TryIndex(_result, out EntityPrototype? prototype);
                if (prototype?.Name != null)
                    _name = prototype.Name;
                return _name;
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
                if (_description.Trim().Length != 0) return _description;
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                protoMan.TryIndex(_result, out EntityPrototype? prototype);
                if (prototype?.Description != null)
                    _description = prototype.Description;
                return _description;
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
        public Dictionary<string, int> RequiredMaterials
        {
            get => _requiredMaterials;
            private set => _requiredMaterials = value;
        }


        /// <summary>
        ///     How many milliseconds it'll take for the lathe to finish this recipe.
        ///     Might lower depending on the lathe's upgrade level.
        /// </summary>
        [ViewVariables]
        public TimeSpan CompleteTime => _completeTime;

        [DataField("applyMaterialDiscount")]
        public bool ApplyMaterialDiscount = true;
    }
}
