using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

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
        private readonly List<PipeNode> _pipes = new List<PipeNode>();

        protected override void OnAddNode(Node node)
        {
            if (!(node is PipeNode pipeNode))
                return;
            _pipes.Add(pipeNode);
            pipeNode.JoinPipeNet(this);
            Air.Volume += pipeNode.Volume;
            Air.Merge(pipeNode.LocalAir);
            pipeNode.LocalAir.Clear();
        }

        protected override void OnRemoveNode(Node node)
        {
            if (!(node is PipeNode pipeNode))
                return;
            var pipeAir = pipeNode.LocalAir;
            pipeAir.Merge(Air);
            pipeAir.Multiply(pipeNode.Volume / Air.Volume);
            _pipes.Remove(pipeNode);
        }

        protected override void OnGivingNodesForCombine(INodeGroup newGroup)
        {
            var newPipeNet = (IPipeNet) newGroup;
            newPipeNet.Air.Merge(Air);
            Air.Clear();
        }

        protected override void AfterRemake(IEnumerable<INodeGroup> newGroups)
        {
            foreach (IPipeNet newPipeNet in newGroups)
            {
                newPipeNet.Air.Merge(Air);
                var newPipeNetGas = newPipeNet.Air;
                newPipeNetGas.Multiply(newPipeNetGas.Volume / Air.Volume);
            }
        }

        private class NullPipeNet : IPipeNet
        {
            GasMixture IGasMixtureHolder.Air { get; set; } = new GasMixture();
        }
    }
}
