using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    /// <summary>
    /// Used for something that can be refined by welder.
    /// For example, glass shard can be refined to glass sheet.
    /// </summary>
    [RegisterComponent]
    public sealed class WelderRefinableComponent : Component
    {
        [DataField("refineResult")]
        public HashSet<string>? RefineResult = new();

        [DataField("refineTime")]
        public float RefineTime = 2f;

        [DataField("refineFuel")]
        public float RefineFuel = 0f;

        [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string QualityNeeded = "Welding";
    }
}
