using Content.Server.DeviceNetwork;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.DeviceNetworkConnections
{
    /// <summary>
    /// Sends and receives device network messages over a wired connection. Devices sending and receiving need to be connected using power power cables. Connections can go through LV, MV and HV wires.
    /// </summary>
    [RegisterComponent]
    public class WiredNetworkConnectionComponent : BaseNetworkConnectionComponent
    {
        public const string WIRENET = "powernet";

        public override string Name => "WiredNetworkConnection";

        protected override int DeviceNetID => NetworkUtils.WIRED;
        protected override int DeviceNetFrequency => 0;

        /// <summary>
        /// Checks if the message was sent from a device that is on the same WireNet.
        /// <seealso cref="NodeGroupID.WireNet"/>
        /// </summary>
        protected override bool CanReceive(int frequency, string sender, NetworkPayload payload, Dictionary<string, object> metadata, bool broadcast)
        {

            if (Owner.TryGetComponent<PowerReceiverComponent>(out var powerReceiver)
                && TryGetWireNet(powerReceiver, out var ownNet)
                && metadata.TryCastValue<INodeGroup>(WIRENET, out var senderNet))
            {
                return ownNet.Equals(senderNet);
            }

            return false;
        }

        protected override Dictionary<string, object> GetMetadata()
        {

            if (Owner.TryGetComponent<PowerReceiverComponent>(out var powerReceiver)
                && TryGetWireNet(powerReceiver, out var net))
            {
                var metadata = new Dictionary<string, object>
                {
                    {WIRENET, net }
                };

                return metadata;
            }

            return new Dictionary<string, object>();
        }

        protected override NetworkPayload ManipulatePayload(NetworkPayload payload)
        {
            return payload;
        }

        /// <summary>
        /// Looks for a node group with the id: <see cref="NodeGroupID.WireNet"/> on the connected power provider.
        /// </summary>
        /// <param name="powerReceiver"></param>
        /// <param name="net"></param>
        /// <returns></returns>
        private static bool TryGetWireNet(PowerReceiverComponent powerReceiver, out INodeGroup net)
        {
            if (powerReceiver.Provider is PowerProviderComponent && powerReceiver.Provider.ProviderOwner.TryGetComponent<NodeContainerComponent>(out var nodeContainer))
            {
                var nodes = nodeContainer.Nodes;
                for (var index = 0; index < nodes.Count; index++)
                {
                    if (nodes[index].NodeGroupID == NodeGroupID.WireNet)
                    {
                        net = nodes[index].NodeGroup;
                        return true;
                    }
                }

            }
            net = default;
            return false;
        }
    }
}
