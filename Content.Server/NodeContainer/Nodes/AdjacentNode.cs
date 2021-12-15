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
            if (!IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Anchored)
                yield break;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var grid = IoCManager.Resolve<IMapManager>().GetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).GridID);
            var gridIndex = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Coordinates);

            foreach (var (_, node) in NodeHelpers.GetCardinalNeighborNodes(entMan, grid, gridIndex))
            {
                if (node != this)
                    yield return node;
            }
        }
    }
}
