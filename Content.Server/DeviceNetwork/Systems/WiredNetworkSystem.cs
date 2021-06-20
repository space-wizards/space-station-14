using Content.Server.DeviceNetwork.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public class WiredNetworkSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WiredNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Tries to get the node groups wit the id: <see cref="NodeGroupID.WireNet"/> from both
        /// the sending and receiving entity and checks if they are the same.
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, WiredNetworkComponent component, BeforePacketSentEvent args)
        {
            if (!component.Owner.TryGetComponent<PowerReceiverComponent>(out var ownPowerReceiver) || !TryGetWireNet(ownPowerReceiver, out var ownNet))
            {
                args.Cancel();
                return;
            }

            if (!ComponentManager.TryGetComponent<PowerReceiverComponent>(args.Sender, out var powerReceiver) || !TryGetWireNet(powerReceiver, out var net))
            {
                args.Cancel();
                return;
            }

            if (!ownNet.Equals(net))
            {
                args.Cancel();
                return;
            }
        }

        /// <summary>
        /// Looks for a node group with the id: <see cref="NodeGroupID.WireNet"/> on the connected power provider.
        /// </summary>
        /// <param name="powerReceiver"></param>
        /// <param name="net"></param>
        /// <returns></returns>
        private bool TryGetWireNet(PowerReceiverComponent powerReceiver, [NotNullWhen(true)] out INodeGroup? net)
        {
            if (powerReceiver.Provider is PowerProviderComponent provider &&
                provider.ProviderOwner.TryGetComponent<NodeContainerComponent>(out var nodeContainer))
            {
                var nodes = nodeContainer.Nodes;

                foreach (var node in nodes.Values)
                {
                    if (node.NodeGroupID == NodeGroupID.WireNet)
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
