#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.NodeContainer.NodeGroups
{
    public interface IPipeNet : INodeGroup, IGasMixtureHolder
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
        public GasMixture Air { get; set; } = new() {Temperature = Atmospherics.T20C};

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
            EntitySystem.Get<AtmosphereSystem>().React(Air, this);
        }

        protected override void OnAddNode(Node node)
        {
            if (node is not PipeNode pipeNode)
                return;

            _pipes.Add(pipeNode);
            pipeNode.JoinPipeNet(this);
            Air.Volume += pipeNode.Volume;
        }

        protected override void OnRemoveNode(Node node)
        {
            RemoveFromGridAtmos();
            if (node is not PipeNode pipeNode)
                return;

            pipeNode.ClearPipeNet();
            _pipes.Remove(pipeNode);
        }

        protected override void OnGivingNodesForCombine(INodeGroup newGroup)
        {
            if (newGroup is not IPipeNet newPipeNet)
                return;

            EntitySystem.Get<AtmosphereSystem>().Merge(newPipeNet.Air, Air);
        }

        protected override void AfterRemake(IEnumerable<INodeGroup> newGroups)
        {
            RemoveFromGridAtmos();

            var buffer = new GasMixture(Air.Volume) {Temperature = Air.Temperature};
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            foreach (var newGroup in newGroups)
            {
                if (newGroup is not IPipeNet newPipeNet)
                    continue;

                var newAir = newPipeNet.Air;

                buffer.Clear();
                atmosphereSystem.Merge(buffer, Air);
                buffer.Multiply(MathF.Min(newAir.Volume / Air.Volume, 1f));
                atmosphereSystem.Merge(newAir, buffer);
            }
        }

        private void RemoveFromGridAtmos()
        {
            GridAtmos?.RemovePipeNet(this);
        }

        private class NullPipeNet : NullNodeGroup, IPipeNet
        {
            private readonly GasMixture _air;

            GasMixture IGasMixtureHolder.Air { get => _air; set { } }

            public NullPipeNet()
            {
                _air = new GasMixture(1f) {Temperature = Atmospherics.T20C};
                _air.MarkImmutable();
            }

            public void Update() { }
        }
    }
}
