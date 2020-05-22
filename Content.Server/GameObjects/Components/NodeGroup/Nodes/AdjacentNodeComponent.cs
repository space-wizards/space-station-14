using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeGroup.Nodes
{
    /// <summary>
    ///     Pulls other AdjacentNodes that are adjacent to it into its node group.
    /// </summary>
    [RegisterComponent]
    public class AdjacentNodeComponent : NodeComponent
    {
        public override string Name => "AdjacentNode";

        protected override IEnumerable<NodeComponent> GetReachableNodes()
        {
            return Owner.GetComponent<SnapGridComponent>()
                .GetCardinalNeighborCells()
                .SelectMany(sgc => sgc.GetLocal())
                .Select(entity => entity.TryGetComponent<AdjacentNodeComponent>(out var node) ? node : null)
                .Where(node => node != null);
        }
    }
}
