using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Stacks
{
    [Prototype("stack")]
    public sealed class StackPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        /// <summary>
        ///     Human-readable name for this stack type e.g. "Steel"
        /// </summary>
        /// <remarks>This is a localization string ID.</remarks>
        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        ///     An icon that will be used to represent this stack type.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier? Icon { get; } = null;

        /// <summary>
        ///     The entity id that will be spawned by default from this stack.
        /// </summary>
        [DataField("spawn", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Spawn { get; } = string.Empty;
    }
}
