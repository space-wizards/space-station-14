using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Tools
{
    [Prototype("tool")]
    public sealed class ToolQualityPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        ///     Human-readable name for this tool quality e.g. "Anchoring"
        /// </summary>
        /// <remarks>This is a localization string ID.</remarks>
        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        ///     Human-readable name for a tool of this type e.g. "Wrench"
        /// </summary>
        /// <remarks>This is a localization string ID.</remarks>
        [DataField("toolName")]
        public string ToolName { get; } = string.Empty;

        /// <summary>
        ///     An icon that will be used to represent this tool type.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier? Icon { get; } = null;

        /// <summary>
        ///     The default entity prototype for this tool type.
        /// </summary>
        [DataField("spawn", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Spawn { get; } = string.Empty;
    }
}
