using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.PipeNet
{
    public class PipeComponent : Component
    {
        public override string Name => "Pipe";

        [ViewVariables]
        public IPipeNet PipeNet { get; private set; }

        [ViewVariables]
        public float Volume { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => Volume, "volume", 10);
        }
    }
}
