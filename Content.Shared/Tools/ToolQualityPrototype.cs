using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Tools
{
    [Prototype("tool")]
    public sealed partial class ToolQualityPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Human-readable name for this tool quality e.g. "Anchoring"
        /// </summary>
        [DataField(required: true)]
        public LocId Name { get; private set; }

        /// <summary>
        ///     Human-readable name for a tool of this type e.g. "Wrench"
        /// </summary>
        [DataField(required: true)]
        public LocId ToolName { get; private set; }

        /// <summary>
        ///     An icon that will be used to represent this tool type.
        /// </summary>
        [DataField]
        public SpriteSpecifier? Icon { get; private set; }

        /// <summary>
        ///     The default entity prototype for this tool type.
        /// </summary>
        [DataField(required: true)]
        public EntProtoId Spawn { get; private set; }
    }
}
