using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.Network
{
    public abstract class NetworkNodeComponent : Component
    {
        [ViewVariables]
        public abstract NetworkType NetworkType { get; }

        [ViewVariables]
        public BaseNetwork Network { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            EnsureHasNetwork();
            SpreadNetwork();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            LeaveCurrentNetwork();
        }

        public bool EnsureHasNetwork()
        {
            if (Network != null)
            {
                return false;
            }

            //try to join a network on a reachable node
            var reachableNetworks = GetReachableNodes().Select(node => node.Network)
                .Where(network => network != null);
            foreach (var network in reachableNetworks)
            {
                if (TrySetNetwork(network))
                {
                    break;
                }
            }

            //if we still dont have a network, make own
            if (Network == null)
            {
                var newNetwork = MakeNewNetwork();
                if (!TrySetNetwork(newNetwork))
                {
                    throw new Exception($"'{this}' could not join '{newNetwork}' that it made for itself.");
                }
            }
            Debug.Assert(Network != null);
            return true;
        }

        public void SpreadNetwork(bool remakingNet = false)
        {
            if (Network == null)
            {
                throw new Exception($"'{this}' tried to spread a network while not having one.");
            }

            var reachableNodes = GetReachableNodes();
            foreach (var node in reachableNodes)
            {
                if (node.TrySetNetworkIfNeeded(Network) && remakingNet)
                {
                    node.SpreadNetwork(remakingNet: true);
                }
            }

            var reachableCompatibleNetworks = reachableNodes.Select(node => node.Network)
                .Where(network => network != null && network != Network && network.NetworkType == NetworkType);

            foreach (var network in reachableCompatibleNetworks)
            {
                network.CombineNetwork(Network);
            }
        }

        public bool TrySetNetwork(BaseNetwork network)
        {
            if (network.NetworkType == NetworkType)
            {
                LeaveCurrentNetwork();
                Network = network;
                Network.AddNode(this);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void LeaveCurrentNetwork()
        {
            Network?.RemoveNode(this);
            Network = null;
        }

        protected abstract IEnumerable<NetworkNodeComponent> GetReachableNodes();

        protected abstract BaseNetwork MakeNewNetwork();

        private bool TrySetNetworkIfNeeded(BaseNetwork network)
        {
            if (Network != null)
            {
                return false;
            }
            else
            {
                return TrySetNetwork(network);
            }
        }
    }
}
