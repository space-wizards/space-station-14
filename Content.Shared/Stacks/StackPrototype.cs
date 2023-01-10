using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Stacks
{
    [Prototype("stack")]
    public sealed class StackPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
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
        public SpriteSpecifier? Icon { get; }

        /// <summary>
        ///     The entity id that will be spawned by default from this stack.
        /// </summary>
        [DataField("spawn", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Spawn { get; } = string.Empty;

        /// <summary>
        ///     The maximum amount of things that can be in a stack.
        ///     Can be overriden on <see cref="StackComponent"/>
        ///     if null, simply has unlimited max count.
        /// </summary>
        [DataField("maxCount")]
        public int? MaxCount { get; }
    }
}
