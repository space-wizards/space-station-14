using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IPipeNet
    {
        GasMixture ContainedGas { get; }
    }

    [NodeGroup(NodeGroupID.Pipe)]
    public class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables]
        public GasMixture ContainedGas { get; private set; } = new GasMixture();

        [ViewVariables]
        private readonly Dictionary<Node, PipeComponent> _pipes = new Dictionary<Node, PipeComponent>();

        public static readonly IPipeNet NullNet = new NullPipeNet();

        protected override void OnAddNode(Node node)
        {
            if (node.Owner.TryGetComponent<PipeComponent>(out var pipe))
            {
                _pipes.Add(node, pipe);
                pipe.JoinPipeNet(this);
                ContainedGas.Volume += pipe.Volume;
                ContainedGas.Merge(pipe.LocalGas);
                pipe.LocalGas.Clear();
            }
        }

        protected override void OnRemoveNode(Node node)
        {
            var pipe = _pipes[node];
            var pipeGas = pipe.LocalGas;

            pipeGas.Merge(ContainedGas);
            pipeGas.Multiply(pipe.Volume / ContainedGas.Volume);
            _pipes.Remove(node);
        }

        protected override void OnGivingNodesForCombine(INodeGroup newGroup)
        {
            var newPipeNet = (IPipeNet) newGroup;
            var newPipeNetGas = newPipeNet.ContainedGas;

            newPipeNetGas.Merge(ContainedGas);
            ContainedGas.Clear();
        }

        protected override void AfterRemake(IEnumerable<INodeGroup> newGroups)
        {
            foreach (IPipeNet newPipeNet in newGroups)
            {
                var newPipeNetGas = newPipeNet.ContainedGas;
                newPipeNetGas.Merge(ContainedGas);
                newPipeNetGas.Multiply(newPipeNetGas.Volume / ContainedGas.Volume);
            }
        }

        private class NullPipeNet : IPipeNet
        {
            public GasMixture ContainedGas => new GasMixture();
        }
    }
}
