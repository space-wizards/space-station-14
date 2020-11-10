using Content.Server.DeviceNetwork;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.DeviceNetworkConnections
{
    public class WiredNetworkConnectionComponent : BaseNetworkConnectionComponent
    {
        public const string WIRENET = "powernet";

        public override string Name => "WiredNetworkConnection";

        protected override int DeviceNetID => NetworkUtils.WIRED;
        protected override int DeviceNetFrequency => 0;

        protected override bool CanReceive(int frequency, string sender, NetworkPayload payload, Metadata metadata, bool broadcast)
        {

            if (Owner.TryGetComponent<PowerReceiverComponent>(out var powerReceiver)
                && TryGetWireNet(powerReceiver, out var ownNet)
                && metadata.TryParseMetadata<INodeGroup>(WIRENET, out var senderNet))
            {
                return ownNet.Equals(senderNet);
            }

            return false;
        }

        protected override Metadata GetMetadata()
        {

            if (Owner.TryGetComponent<PowerReceiverComponent>(out var powerReceiver)
                && TryGetWireNet(powerReceiver, out var net))
            {
                var metadata = new Metadata
                {
                    {WIRENET, net }
                };

                return metadata;
            }

            return new Metadata();
        }

        protected override NetworkPayload ManipulatePayload(NetworkPayload payload)
        {
            return payload;
        }

        private bool TryGetWireNet(PowerReceiverComponent powerReceiver, out INodeGroup net)
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
