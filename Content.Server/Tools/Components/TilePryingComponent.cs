using System.Threading;
using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components
{
    [RegisterComponent]
    public sealed partial class TilePryingComponent : Component
    {
        [DataField("toolComponentNeeded")]
        public bool ToolComponentNeeded = true;

        [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string QualityNeeded = "Prying";

        /// <summary>
        /// Whether this tool can pry tiles with CanAxe.
        /// </summary>
        [DataField("advanced")]
        public bool Advanced = false;

        [DataField("delay")]
        public float Delay = 1f;
    }
}
