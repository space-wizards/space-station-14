using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;

namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [Access(typeof(ApcNetworkSystem))]
    [ComponentProtoName("ApcNetworkConnection")]
    public sealed partial class ApcNetworkComponent : Component
    {
        /// <summary>
        /// The node Group the ApcNetworkConnection is connected to
        /// </summary>
        [ViewVariables] public Node? ConnectedNode;
    }
}
