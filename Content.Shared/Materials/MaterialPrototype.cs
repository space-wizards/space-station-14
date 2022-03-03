using Content.Shared.Stacks;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

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
        [DataField("parent")]
        public string? Parent { get; } = null;

        [ViewVariables]
        [DataField("abstract")]
        public bool Abstract { get; } = false;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("stack", customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        public string? StackId { get; } = null;

        [ViewVariables]
        [DataField("name")]
        public string Name { get; } = "unobtanium";

        [ViewVariables]
        [DataField("color")]
        public Color Color { get; } = Color.Gray;

        /// <summary>
        ///     An icon used to represent the material in graphic interfaces.
        /// </summary>
        [ViewVariables]
        [DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;
    }
}
