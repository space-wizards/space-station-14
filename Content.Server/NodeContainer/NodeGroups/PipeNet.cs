using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
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

        [ViewVariables] private AtmosphereSystem? _atmosphereSystem;

        public EntityUid? Grid { get; private set; }

        public override void Initialize(Node sourceNode, IEntityManager entMan)
        {
            base.Initialize(sourceNode, entMan);

            Grid = entMan.GetComponent<TransformComponent>(sourceNode.Owner).GridUid;

            if (Grid == null)
            {
                // This is probably due to a cannister or something like that being spawned in space.
                return;
            }

            _atmosphereSystem = entMan.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
            _atmosphereSystem.AddPipeNet(Grid.Value, this);
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
                Air.Volume += pipeNode.Volume;
            }
        }

        public override void RemoveNode(Node node)
        {
            base.RemoveNode(node);

            // if the node is simply being removed into a separate group, we do nothing, as gas redistribution will be
            // handled by AfterRemake(). But if it is being deleted, we actually want to remove the gas stored in this node.
            if (!node.Deleting || node is not PipeNode pipe)
                return;

            Air.Multiply(1f - pipe.Volume / Air.Volume);
            Air.Volume -= pipe.Volume;
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            RemoveFromGridAtmos();

            var newAir = new List<GasMixture>(newGroups.Count());
            foreach (var newGroup in newGroups)
            {
                if (newGroup.Key is IPipeNet newPipeNet)
                    newAir.Add(newPipeNet.Air);
            }

            _atmosphereSystem?.DivideInto(Air, newAir);
        }

        private void RemoveFromGridAtmos()
        {
            if (Grid == null)
                return;

            _atmosphereSystem?.RemovePipeNet(Grid.Value, this);
        }

        public override string GetDebugData()
        {
            return @$"Pressure: { Air.Pressure:G3}
Temperature: {Air.Temperature:G3}
Volume: {Air.Volume:G3}";
        }
    }
}
