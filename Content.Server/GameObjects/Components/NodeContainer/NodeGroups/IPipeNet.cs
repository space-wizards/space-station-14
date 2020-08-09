using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Transactions;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IPipeNet : IGasMixtureHolder
    {

    }

    [NodeGroup(NodeGroupID.Pipe)]
    public class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables]
        public GasMixture Air { get; set; } = new GasMixture();

        public static readonly IPipeNet NullNet = new NullPipeNet();

        [ViewVariables]
        private readonly Dictionary<Node, PipeComponent> _pipes = new Dictionary<Node, PipeComponent>();

        public bool AssumeAir(GasMixture giver)
        {
            Air.Merge(giver);
            return true;
        }

        protected override void OnAddNode(Node node)
        {
            if (node.Owner.TryGetComponent<PipeComponent>(out var pipe))
            {
                _pipes.Add(node, pipe);
                pipe.JoinPipeNet(this);
                Air.Volume += pipe.Volume;
                AssumeAir(pipe.Air);
                pipe.Air.Clear();
            }
        }

        protected override void OnRemoveNode(Node node)
        {
            var pipe = _pipes[node];
            pipe.AssumeAir(Air);
            pipe.Air.Multiply(pipe.Volume / Air.Volume);
            _pipes.Remove(node);
        }

        protected override void OnGivingNodesForCombine(INodeGroup newGroup)
        {
            var newPipeNet = (IPipeNet) newGroup;
            newPipeNet.AssumeAir(Air);
            Air.Clear();
        }

        protected override void AfterRemake(IEnumerable<INodeGroup> newGroups)
        {
            foreach (IPipeNet newPipeNet in newGroups)
            {
                newPipeNet.AssumeAir(Air);
                var newPipeNetGas = newPipeNet.Air;
                newPipeNetGas.Multiply(newPipeNetGas.Volume / Air.Volume);
            }
        }

        private class NullPipeNet : IPipeNet
        {
            GasMixture IGasMixtureHolder.Air { get; set; } = new GasMixture();

            public bool AssumeAir(GasMixture giver)
            {
                return false;
            }
        }
    }
}
