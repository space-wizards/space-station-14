using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Map.Components;

namespace Content.Server.Spreader;

/// <summary>
/// Handles the node for <see cref="EdgeSpreaderComponent"/>.
/// Functions as a generic tile-based entity spreader for systems such as puddles or smoke.
/// </summary>
public sealed class SpreaderNode : Node
{
    /// <inheritdoc/>
    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform, EntityQuery<NodeContainerComponent> nodeQuery, EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid, IEntityManager entMan)
    {
        if (grid == null)
            yield break;

        entMan.System<SpreaderSystem>().GetNeighbors(xform.Owner, Name, out _, out _, out var neighbors);

        foreach (var neighbor in neighbors)
        {
            if (!nodeQuery.TryGetComponent(neighbor, out var nodeContainer) ||
                !nodeContainer.TryGetNode<SpreaderNode>(Name, out var neighborNode))
            {
                continue;
            }

            yield return neighborNode;
        }
    }
}
