using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Materials
{
    /// <summary>
    ///     Materials are read-only storage for the properties of specific materials.
    ///     Properties should be intrinsic (or at least as much is necessary for game purposes).
    /// </summary>
    [Prototype("material")]
    public sealed class MaterialPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<MaterialPrototype>))]
        public string[]? Parents { get; }

        [ViewVariables]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; } = false;

        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     For material storage to be able to convert back and forth
        ///     between the material and physical entities you can carry,
        ///     include which stack we should spawn by default.
        /// </summary>
        [DataField("stackEntity", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? StackEntity;

        [DataField("name")]
        public string Name = string.Empty;

        /// <summary>
        /// Locale id for the unit of this material.
        /// Lathe recipe tooltips and material storage display use this to let you change a material to sound nicer.
        /// For example, 5 bars of gold is better than 5 sheets of gold.
        /// </summary>
        [DataField("unit")]
        public string Unit = "materials-unit-sheet";

        [DataField("color")]
        public Color Color { get; private set; } = Color.Gray;

        /// <summary>
        ///     An icon used to represent the material in graphic interfaces.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

        /// <summary>
        /// The price per cm3.
        /// </summary>
        [DataField("price", required: true)]
        public double Price = 0;
    }
}
