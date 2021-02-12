#nullable enable
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    public class GasCanisterPortComponent : Component
    {
        public override string Name => "GasCanisterPort";

        [ViewVariables]
        public GasCanisterComponent? ConnectedCanister { get; private set; }

        [ViewVariables]
        public bool ConnectedToCanister => ConnectedCanister != null;

        [ViewVariables]
        private PipeNode? _gasPort;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PipeNetDeviceComponent>();
            SetGasPort();
            if (Owner.TryGetComponent<SnapGridComponent>(out var snapGrid))
            {
                var entities = snapGrid.GetLocal();
                foreach (var entity in entities)
                {
                    if (entity.TryGetComponent<GasCanisterComponent>(out var canister) && canister.Anchored && !canister.ConnectedToPort)
                    {
                        canister.TryConnectToPort();
                        break;
                    }
                }
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            ConnectedCanister?.DisconnectFromPort();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PipeNetUpdateMessage:
                    Update();
                    break;
            }
        }

        public void Update()
        {
            if (_gasPort == null || ConnectedCanister == null)
                return;

            ConnectedCanister.Air.Share(_gasPort.Air, 1);
            ConnectedCanister.AirWasUpdated();
        }

        public void ConnectGasCanister(GasCanisterComponent gasCanister)
        {
            ConnectedCanister = gasCanister;
        }

        public void DisconnectGasCanister()
        {
            ConnectedCanister = null;
        }

        private void SetGasPort()
        {
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                Logger.Error($"{nameof(GasCanisterPortComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            _gasPort = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (_gasPort == null)
            {
                Logger.Error($"{nameof(GasCanisterPortComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }
    }
}
