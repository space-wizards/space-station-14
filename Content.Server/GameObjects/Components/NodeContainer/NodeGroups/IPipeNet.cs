#nullable enable
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IPipeNet : IGasMixtureHolder
    {
        /// <summary>
        ///     Causes gas in the PipeNet to react.
        /// </summary>
        void Update();
    }

    [NodeGroup(NodeGroupID.Pipe)]
    public class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables]
        public GasMixture Air { get; set; } = new();

        public static readonly IPipeNet NullNet = new NullPipeNet();

        [ViewVariables]
        private readonly List<PipeNode> _pipes = new();

        [ViewVariables] private AtmosphereSystem? _atmosphereSystem;

        [ViewVariables] private IGridAtmosphereComponent? GridAtmos => _atmosphereSystem?.GetGridAtmosphere(GridId);

        public override void Initialize(Node sourceNode)
        {
            base.Initialize(sourceNode);

            _atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            GridAtmos?.AddPipeNet(this);
        }

        public void Update()
        {
            Air.React(this);
        }

        protected override void OnAddNode(Node node)
        {
            if (node is not PipeNode pipeNode)
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
            if (node is not PipeNode pipeNode)
                return;
            var pipeAir = pipeNode.LocalAir;
            pipeAir.Merge(Air);
            pipeAir.Multiply(pipeNode.Volume / Air.Volume);
            _pipes.Remove(pipeNode);
        }

        protected override void OnGivingNodesForCombine(INodeGroup newGroup)
        {
            if (newGroup is not IPipeNet newPipeNet)
                return;
            newPipeNet.Air.Merge(Air);
            Air.Clear();
        }

        protected override void AfterRemake(IEnumerable<INodeGroup> newGroups)
        {
            foreach (var newGroup in newGroups)
            {
                if (newGroup is not IPipeNet newPipeNet)
                    continue;
                newPipeNet.Air.Merge(Air);
                var newPipeNetGas = newPipeNet.Air;
                newPipeNetGas.Multiply(newPipeNetGas.Volume / Air.Volume);
            }
            RemoveFromGridAtmos();
        }

        private void RemoveFromGridAtmos()
        {
            GridAtmos?.RemovePipeNet(this);
        }

        private class NullPipeNet : IPipeNet
        {
            GasMixture IGasMixtureHolder.Air { get; set; } = new();
            public void Update() { }
        }
    }
}
