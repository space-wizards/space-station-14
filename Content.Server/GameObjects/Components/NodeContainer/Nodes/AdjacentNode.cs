#nullable enable
using System.Collections.Generic;
using Robust.Shared.GameObjects;
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
            if (!Owner.TryGetComponent(out SnapGridComponent? grid))
                yield break;

            var cells = grid.GetCardinalNeighborCells();

            foreach (var cell in cells)
            {
                foreach (var entity in cell.GetLocal())
                {
                    if (entity.TryGetComponent<NodeContainerComponent>(out var container))
                    {
                        foreach (var node in container.Nodes)
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
}
