using Content.Shared.Localizations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Tools
{
    [Prototype("tool")]
    public sealed partial class ToolQualityPrototype : IPrototype, ISerializationHooks
    {
        private ILocalizationManager _loc = default!;

        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("name")]
        private string _name = string.Empty;

        /// <summary>
        ///     Human-readable name for this tool quality e.g. "Anchoring"
        /// </summary>
        public string Name => _loc.GetString(_name);

        [DataField("toolName")]
        private string _toolName = string.Empty;

        /// <summary>
        ///     Human-readable name for a tool of this type e.g. "Wrench"
        /// </summary>
        public string ToolName => _loc.GetString(_toolName);

        /// <summary>
        ///     An icon that will be used to represent this tool type.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier? Icon { get; private set; } = null;

        /// <summary>
        ///     The default entity prototype for this tool type.
        /// </summary>
        [DataField("spawn", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Spawn { get; private set; } = string.Empty;

        void ISerializationHooks.AfterDeserialization()
        {
            _loc = IoCManager.Resolve<ILocalizationManager>();
        }
    }
}
