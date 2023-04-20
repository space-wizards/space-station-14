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
        public string ID { get; } = default!;

        /// <summary>
        ///     For material storage to be able to convert back and forth
        ///     between the material and physical entities you can carry,
        ///     include which stack we should spawn by default.
        /// </summary>
        [DataField("stackEntity", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string StackEntity { get; } = "";

        [DataField("name")]
        public string Name = "";

        [DataField("color")]
        public Color Color { get; } = Color.Gray;

        /// <summary>
        ///     An icon used to represent the material in graphic interfaces.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        /// The price per cm3.
        /// </summary>
        [DataField("price", required: true)]
        public double Price = 0;
    }
}
