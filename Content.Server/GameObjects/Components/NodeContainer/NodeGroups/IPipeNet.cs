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
        private readonly List<PipeComponent> _pipes = new List<PipeComponent>();

        public static readonly IPipeNet NullNet = new NullPipeNet();

        protected override void OnAddNode(Node node)
        {
            if (node.Owner.TryGetComponent<PipeComponent>(out var pipe))
            {
                _pipes.Add(pipe);
                pipe.JoinPipeNet(this);
                ContainedGas.Merge(pipe.LocalGas);
                pipe.LocalGas.Clear();
            }
        }

        protected override void OnRemoveNode(Node node)
        {
            foreach (var pipe in _pipes)
            {
                pipe.ClearPipeNet();
                pipe.LocalGas.Merge(ContainedGas);
                pipe.LocalGas.Multiply(pipe.LocalGas.Volume / ContainedGas.Volume);
                ContainedGas.Clear();
            }
        }

        private class NullPipeNet : IPipeNet
        {
            public GasMixture ContainedGas => _containedGas;
            private static GasMixture _containedGas = new GasMixture();
        }
    }
}
