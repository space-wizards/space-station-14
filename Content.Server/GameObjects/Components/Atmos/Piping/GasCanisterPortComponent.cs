using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    public class GasCanisterPortComponent : PipeNetDeviceComponent
    {
        public override string Name => "GasCanisterPort";

        [ViewVariables]
        public bool GasCanisterConnected { get; private set; }

        [ViewVariables]
        public GasCanisterComponent ConnectedGasCanister { get; private set; }

        [ViewVariables]
        private PipeNode _gasPort;

        public override void Initialize()
        {
            base.Initialize();

        }

        public override void Update()
        {
            if (GasCanisterConnected)
            {
                ConnectedGasCanister.Air.Share(_gasPort.Air, 0);
            }
        }
    }
}
