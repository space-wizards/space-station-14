using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Prototypes
{
    [NetSerializable, Serializable, Prototype("technology")]
    public sealed class TechnologyPrototype : IPrototype
    {
        /// <summary>
        ///     The ID of this technology prototype.
        /// </summary>
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name this technology will have on user interfaces.
        /// </summary>
        [DataField("name")]
        public string Name
        {
            get => (_name is not null) ? _name : ID;
            private set => _name = Loc.GetString(value);
        }

        private string? _name;

        /// <summary>
        ///     An icon that represent this technology.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     A short description of the technology.
        /// </summary>
        [DataField("description")]
        public string Description
        {
            get => (_description is not null) ? _description : "";
            private set => _description = Loc.GetString(value);
        }

        private string? _description;

        /// <summary>
        ///    The required research points to unlock this technology.
        /// </summary>
        [DataField("requiredPoints")]
        public int RequiredPoints { get; }

        /// <summary>
        ///     A list of technology IDs required to unlock this technology.
        /// </summary>
        [DataField("requiredTechnologies", customTypeSerializer: typeof(PrototypeIdListSerializer<TechnologyPrototype>))]
        public List<string> RequiredTechnologies { get; } = new();

        /// <summary>
        ///     A list of recipe IDs this technology unlocks.
        /// </summary>
        [DataField("unlockedRecipes", customTypeSerializer:typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        public List<string> UnlockedRecipes { get; } = new();
    }
}
