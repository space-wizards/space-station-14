using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Power.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.DeviceNetwork.Connections
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

            if (_owner.TryGetComponent<ApcPowerReceiverComponent>(out var powerReceiver)
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

            if (_owner.TryGetComponent<ApcPowerReceiverComponent>(out var powerReceiver)
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

        private bool TryGetWireNet(ApcPowerReceiverComponent apcPowerReceiver, [NotNullWhen(true)] out INodeGroup? net)
        {
            var provider = apcPowerReceiver.Provider;
            if (provider != null && provider.ProviderOwner.TryGetComponent<NodeContainerComponent>(out var nodeContainer))
            {
                var nodes = nodeContainer.Nodes;

                foreach (var node in nodes.Values)
                {
                    if (node.NodeGroupID == NodeGroupID.WireNet && node.NodeGroup != null)
                    {
                        net = node.NodeGroup;
                        return true;
                    }
                }
            }

            net = default;
            return false;
        }
    }
}
