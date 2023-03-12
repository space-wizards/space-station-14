using System.Threading;
using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components
{
    [RegisterComponent]
    public sealed class TilePryingComponent : Component
    {
        [DataField("toolComponentNeeded")]
        public bool ToolComponentNeeded = true;

        [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string QualityNeeded = "Prying";

        [DataField("delay")]
        public float Delay = 1f;
    }
}
