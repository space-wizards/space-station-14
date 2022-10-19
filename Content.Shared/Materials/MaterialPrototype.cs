using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Materials
{
    /// <summary>
    ///     Materials are read-only storage for the properties of specific materials.
    ///     Properties should be intrinsic (or at least as much is necessary for game purposes).
    /// </summary>
    [Prototype("material")]
    public readonly record struct MaterialPrototype : IPrototype, IInheritingPrototype
    {
        private readonly string _name = string.Empty;

        [ViewVariables]
        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<MaterialPrototype>))]
        public string[]? Parents { get; }

        [ViewVariables]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; } = false;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("stack", customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        public string? StackId { get; } = null;

        [ViewVariables] [DataField("name", customTypeSerializer: typeof(LocStringSerializer))]
        public readonly string Name = string.Empty;

        [ViewVariables]
        [DataField("color")]
        public Color Color { get; } = Color.Gray;

        /// <summary>
        ///     An icon used to represent the material in graphic interfaces.
        /// </summary>
        [ViewVariables]
        [DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        /// The price per cm3.
        /// </summary>
        [DataField("price", required: true)] public readonly double Price;
    }
}
