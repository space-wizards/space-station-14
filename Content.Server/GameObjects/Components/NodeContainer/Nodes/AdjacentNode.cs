#nullable enable
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     A <see cref="Node"/> that can reach other <see cref="AdjacentNode"/>s that are directly adjacent to it.
    /// </summary>
    public class AdjacentNode : Node
    {
        protected override IEnumerable<Node> GetReachableNodes()
        {
            var cells = Owner.GetComponent<SnapGridComponent>()
                .GetCardinalNeighborCells();

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
