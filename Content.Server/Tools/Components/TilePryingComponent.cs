using System.Threading;
using Content.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Tools.Components
{
    [RegisterComponent, ComponentProtoName("TilePrying")]
    public sealed class TilePryingComponent : Component
    {
        [ViewVariables]
        [DataField("toolComponentNeeded")]
        public bool ToolComponentNeeded = true;

        [ViewVariables]
        [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string QualityNeeded = "Prying";

        [ViewVariables]
        [DataField("delay")]
        public float Delay = 1f;

        /// <summary>
        /// Used for do_afters.
        /// </summary>
        public CancellationTokenSource? CancelToken = null;
    }
}
