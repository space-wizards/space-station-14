using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public class WiredNetworkConnection : BaseNetworkConnection
    {
        public const string WIRENET = "powernet";

        private readonly IEntity _owner;

        public WiredNetworkConnection(OnReceiveNetMessage onReceive, bool receiveAll, IEntity owner) : base(NetworkUtils.WIRED, 0, onReceive, receiveAll)
        {
            _owner = owner;
        }

        protected override bool CanReceive(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast)
        {
            if (_owner.Deleted)
            {
                Connection.Close();
                return false;
            }

            if (_owner.TryGetComponent<PowerReceiverComponent>(out var powerReceiver)
                && TryGetWireNet(powerReceiver, out var ownNet)
                && metadata.TryParseMetadata<INodeGroup>(WIRENET, out var senderNet))
            {
                return ownNet.Equals(senderNet);
            }

            return false;
        }

        protected override Metadata GetMetadata()
        {
            if (_owner.Deleted)
            {
                Connection.Close();
                return new Metadata();
            }

            if (_owner.TryGetComponent<PowerReceiverComponent>(out var powerReceiver)
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

        protected override Dictionary<string, string> ManipulatePayload(Dictionary<string, string> payload)
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
