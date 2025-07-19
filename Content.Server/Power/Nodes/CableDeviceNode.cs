using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes
{
    /// <summary>
    ///     Type of node that connects to a <see cref="CableNode"/> below it.
    /// </summary>
    [DataDefinition]
    [Virtual]
    public partial class CableDeviceNode : Node
    {
        /// <summary>
        /// If disabled, this cable device will never connect.
        /// </summary>
        /// <remarks>
        /// If you change this,
        /// you must manually call <see cref="NodeGroupSystem.QueueReflood"/> to update the node connections.
        /// </remarks>
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;

        public override bool Connectable(IEntityManager entMan, TransformComponent? xform = null)
        {
            if (!Enabled)
                return false;

            return base.Connectable(entMan, xform);
        }

        public override IEnumerable<Node> GetReachableNodes(Entity<TransformComponent> xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            Entity<MapGridComponent>? grid,
            IEntityManager entMan)
        {
            if (!xform.Comp.Anchored || grid is not { } gridEnt)
                yield break;

            var mapSystem = entMan.System<SharedMapSystem>();
            var gridIndex = mapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, gridEnt, gridIndex, mapSystem))
            {
                if (node is CableNode)
                    yield return node;
            }
        }
    }
}
