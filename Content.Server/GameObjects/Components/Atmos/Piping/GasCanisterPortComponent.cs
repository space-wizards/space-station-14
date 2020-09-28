using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    public class GasCanisterPortComponent : PipeNetDeviceComponent
    {
        public override string Name => "GasCanisterPort";

        [ViewVariables]
        public GasCanisterComponent ConnectedGasCanister { get; private set; }

        [ViewVariables]
        private PipeNode _gasPort;

        public override void Initialize()
        {
            base.Initialize();
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                JoinedGridAtmos?.RemovePipeNetDevice(this);
                Logger.Error($"{typeof(GasCanisterPortComponent)} on entity {Owner.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            _gasPort = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (_gasPort == null)
            {
                JoinedGridAtmos?.RemovePipeNetDevice(this);
                Logger.Error($"{typeof(GasCanisterPortComponent)} on entity {Owner.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }

        public override void Update()
        {
            ConnectedGasCanister?.Air.Share(_gasPort.Air, 0);
        }

        public void ConnectGasCanister(GasCanisterComponent gasCanister)
        {
            ConnectedGasCanister = gasCanister;
        }

        public void DisconnectGasCanister()
        {
            ConnectedGasCanister = null;
        }
    }
}
