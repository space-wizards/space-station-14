using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.PipeNet
{
    public class PumpComponent : Component
    {
        public override string Name => "Pump";

        private PipeDirection _inletDirection;

        private PipeDirection _outletDirection;

        private PipeNode _inletNode;

        private PipeNode _outletNode;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _inletDirection, "inletDirection", PipeDirection.None);
            serializer.DataField(ref _outletDirection, "outletDirection", PipeDirection.None);
        }

        public override void Initialize()
        {
            base.Initialize();
            var pipeNodes = Owner.GetComponent<NodeContainerComponent>().Nodes.OfType<PipeNode>();
            _inletNode = pipeNodes.Where(pipeNode => pipeNode.PipeDirection == _inletDirection).First();
            _outletNode = pipeNodes.Where(pipeNode => pipeNode.PipeDirection == _outletDirection).First();
        }

        public void Update(float frameTime)
        {
        }
    }
}
