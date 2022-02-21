using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Utility;

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
    public sealed class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables] public GasMixture Air { get; set; } = new() {Temperature = Atmospherics.T20C};

        [ViewVariables] private readonly List<PipeNode> _pipes = new();

        [ViewVariables] private AtmosphereSystem? _atmosphereSystem;

        public GridId Grid => GridId;

        public override void Initialize(Node sourceNode)
        {
            base.Initialize(sourceNode);

            _atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            _atmosphereSystem.AddPipeNet(this);
        }

        public void Update()
        {
            _atmosphereSystem?.React(Air, this);
        }

        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);

            foreach (var node in groupNodes)
            {
                var pipeNode = (PipeNode) node;
                _pipes.Add(pipeNode);
                Air.Volume += pipeNode.Volume;
            }
        }

        public override void RemoveNode(Node node)
        {
            base.RemoveNode(node);

            var pipeNode = (PipeNode) node;
            Air.Volume -= pipeNode.Volume;
            // TODO: Bad O(n^2)
            _pipes.Remove(pipeNode);
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            RemoveFromGridAtmos();

            var buffer = new GasMixture(Air.Volume) {Temperature = Air.Temperature};
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            foreach (var newGroup in newGroups)
            {
                if (newGroup.Key is not IPipeNet newPipeNet)
                    continue;

                var newAir = newPipeNet.Air;
                var newVolume = newGroup.Cast<PipeNode>().Sum(n => n.Volume);

                buffer.Clear();
                atmosphereSystem.Merge(buffer, Air);
                buffer.Multiply(MathF.Min(newVolume / Air.Volume, 1f));
                atmosphereSystem.Merge(newAir, buffer);
            }
        }

        private void RemoveFromGridAtmos()
        {
            DebugTools.AssertNotNull(_atmosphereSystem);
            _atmosphereSystem?.RemovePipeNet(this);
        }

        public override string GetDebugData()
        {
            return @$"Pressure: { Air.Pressure:G3}
Temperature: {Air.Temperature:G3}
Volume: {Air.Volume:G3}";
        }
    }
}
