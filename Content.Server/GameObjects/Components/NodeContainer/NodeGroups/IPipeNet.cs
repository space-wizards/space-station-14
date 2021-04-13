#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
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
            Air.React(this);
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

            newPipeNet.Air.Merge(Air);
        }

        protected override void AfterRemake(IEnumerable<INodeGroup> newGroups)
        {
            RemoveFromGridAtmos();

            var buffer = new GasMixture(Air.Volume) {Temperature = Air.Temperature};

            foreach (var newGroup in newGroups)
            {
                if (newGroup is not IPipeNet newPipeNet)
                    continue;

                var newAir = newPipeNet.Air;

                buffer.Clear();
                buffer.Merge(Air);
                buffer.Multiply(MathF.Min(newAir.Volume / Air.Volume, 1f));
                newAir.Merge(buffer);
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
