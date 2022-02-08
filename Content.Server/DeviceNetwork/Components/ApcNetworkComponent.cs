using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [Friend(typeof(ApcNetworkSystem))]
    [ComponentProtoName("ApcNetworkConnection")]
    public class ApcNetworkComponent : Component
    {
        /// <summary>
        /// The node Group the ApcNetworkConnection is connected to
        /// </summary>
        [ViewVariables] public Node? ConnectedNode;
    }
}
