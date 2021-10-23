using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [Friend(typeof(ApcNetworkSystem))]
    public class ApcNetworkComponent : Component
    {
        public override string Name => "ApcNetworkConnection";

        /// <summary>
        /// The node Group the ApcNetworkConnection is connected to
        /// </summary>
        [ViewVariables] public Node? ConnectedNode;
    }
}
