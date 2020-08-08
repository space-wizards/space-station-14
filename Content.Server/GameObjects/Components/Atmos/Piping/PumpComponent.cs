using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class PumpComponent : Component
    {
        public override string Name => "Pump";

        [ViewVariables]
        private PipeDirection _inletDirection;

        [ViewVariables]
        private PipeDirection _outletDirection;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _inletDirection, "inletDirection", PipeDirection.None);
            serializer.DataField(ref _outletDirection, "outletDirection", PipeDirection.None);
        }

        public void Update(float frameTime)
        {
        }
    }
}
