using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.Network
{
    public abstract class BaseNetwork
    {
        [ViewVariables]
        public abstract NetworkType NetworkType { get; }

        [ViewVariables]
        public IReadOnlyList<NetworkNodeComponent> Nodes => _nodes;
        private readonly List<NetworkNodeComponent> _nodes = new List<NetworkNodeComponent>();

        [ViewVariables]
        public int NodeCount => Nodes.Count;

        private bool _recombining = false;

        public void AddNode(NetworkNodeComponent node)
        {
            Debug.Assert(node.NetworkType == NetworkType, $"'{this}' is not compatible with '{node}'.");
            _nodes.Add(node);
        }

        public void RemoveNode(NetworkNodeComponent node)
        {
            Debug.Assert(_nodes.Contains(node), $"'{this}' cannot remove '{node}', as it was not in this network.");
            _nodes.Remove(node);
            RemakeNetwork();
        }

        public void CombineNetwork(BaseNetwork network)
        {
            Debug.Assert(network.NetworkType == NetworkType);
            _recombining = true;
            while (Nodes.Any())
            {
                var node = Nodes.First();
                node.TrySetNetwork(network);
                Debug.Assert(network.Nodes.Contains(node));
            }
            //This should now be GC-able
        }

        private void RemakeNetwork()
        {
            if (_recombining)
            {
                return;
            }
            _recombining = true;
            var nodes = Nodes.ToArray();
            while (Nodes.Any())
            {
                Nodes.First().LeaveCurrentNetwork();
            }
            foreach (var node in nodes)
            {
                if (node.EnsureHasNetwork())
                {
                    node.SpreadNetwork(remakingNet: true);
                }
            }
            //This should now be GC-able
        }
    }

    public enum NetworkType
    {
        LVPower,
        MVPower,
        HVPower,
    }
}
