#nullable enable
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     A <see cref="Node"/> that can reach other <see cref="AdjacentNode"/>s that are directly adjacent to it.
    /// </summary>
    [DataDefinition]
    public class AdjacentNode : Node
    {
        protected override IEnumerable<Node> GetReachableNodes()
        {
            if (!Owner.Transform.Anchored)
                yield break;

            var grid = IoCManager.Resolve<IMapManager>().GetGrid(Owner.Transform.GridID);
            var coords = Owner.Transform.Coordinates;
            foreach (var cell in grid.GetCardinalNeighborCells(coords))
            {
                foreach (var entity in grid.GetLocal(Owner.EntityManager.GetEntity(cell).Transform.Coordinates))
                {
                    if (!Owner.EntityManager.GetEntity(entity).TryGetComponent<NodeContainerComponent>(out var container))
                        continue;

                    foreach (var node in container.Nodes.Values)
                    {
                        if (node != null && node != this)
                        {
                            yield return node;
                        }
                    }

                }
            }
        }
    }
}
