using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IPipeNet : IGasMixtureHolder
    {
        void Update();

        /// <summary>
        ///     If the <see cref="IGridAtmosphereComponent"/> this is being updates by should continue to do so,
        ///     or get rid of this from its queue.
        /// </summary>
        bool ContinueAtmosUpdates { get; }
    }

    [NodeGroup(NodeGroupID.Pipe)]
    public class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables]
        public GasMixture Air { get; set; } = new GasMixture();

        public bool ContinueAtmosUpdates { get; private set; } = true;

        public static readonly IPipeNet NullNet = new NullPipeNet();

        [ViewVariables]
        private readonly List<PipeNode> _pipes = new List<PipeNode>();

        public override void Initialize(Node sourceNode)
        {
            base.Initialize(sourceNode);
            EntitySystem.Get<AtmosphereSystem>()
                .GetGridAtmosphere(GridId)
                ?.AddPipeNet(this);
        }

        public void Update()
        {
            Air.React(this);
        }

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
            RemoveFromGridAtmos();
            if (!(node is PipeNode pipeNode))
                return;
            var pipeAir = pipeNode.LocalAir;
            pipeAir.Merge(Air);
            pipeAir.Multiply(pipeNode.Volume / Air.Volume);
            _pipes.Remove(pipeNode);
        }

        protected override void OnGivingNodesForCombine(INodeGroup newGroup)
        {
            if (!(newGroup is IPipeNet newPipeNet))
                return;
            newPipeNet.Air.Merge(Air);
            Air.Clear();
        }

        protected override void AfterRemake(IEnumerable<INodeGroup> newGroups)
        {
            foreach (var newGroup in newGroups)
            {
                if (!(newGroup is IPipeNet newPipeNet))
                    continue;
                newPipeNet.Air.Merge(Air);
                var newPipeNetGas = newPipeNet.Air;
                newPipeNetGas.Multiply(newPipeNetGas.Volume / Air.Volume);
            }
            RemoveFromGridAtmos();
        }

        private void RemoveFromGridAtmos()
        {
            ContinueAtmosUpdates = false;
        }

        private class NullPipeNet : IPipeNet
        {
            public bool ContinueAtmosUpdates => false;
            GasMixture IGasMixtureHolder.Air { get; set; } = new GasMixture();
            public void Update() { }
        }
    }
}
