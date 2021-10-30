using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.NodeContainer.Nodes
{
    /// <summary>
    ///     A <see cref="Node"/> that can reach other <see cref="AdjacentNode"/>s that are directly adjacent to it.
    /// </summary>
    [DataDefinition]
    public class AdjacentNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes()
        {
            if (!Owner.Transform.Anchored)
                yield break;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var grid = IoCManager.Resolve<IMapManager>().GetGrid(Owner.Transform.GridID);
            var gridIndex = grid.TileIndicesFor(Owner.Transform.Coordinates);

            foreach (var (_, node) in NodeHelpers.GetCardinalNeighborNodes(entMan, grid, gridIndex))
            {
                if (node != this)
                    yield return node;
            }
        }
    }
}
