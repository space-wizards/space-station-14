using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    public class GasCanisterPortComponent : PipeNetDeviceComponent
    {
        public override string Name => "GasCanisterPort";

        [ViewVariables]
        public GasCanisterComponent ConnectedCanister { get; private set; }

        [ViewVariables]
        public bool ConnectedToCanister => ConnectedCanister != null;

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
            if (Owner.TryGetComponent<SnapGridComponent>(out var snapGrid))
            {
                var anchoredCanister = snapGrid.GetLocal()
                    .Select(entity => entity.TryGetComponent<GasCanisterComponent>(out var canister) ? canister : null)
                    .Where(canister => canister != null)
                    .Where(canister => canister.Anchored)
                    .Where(canister => !canister.ConnectedToPort)
                    .FirstOrDefault();
                if (anchoredCanister != null)
                {
                    anchoredCanister.TryConnectToPort();
                }
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            ConnectedCanister?.DisconnectFromPort();
        }

        public override void Update()
        {
            ConnectedCanister?.Air.Share(_gasPort.Air, 1);
            ConnectedCanister?.AirWasUpdated();
        }

        public void ConnectGasCanister(GasCanisterComponent gasCanister)
        {
            ConnectedCanister = gasCanister;
        }

        public void DisconnectGasCanister()
        {
            ConnectedCanister = null;
        }
    }
}
