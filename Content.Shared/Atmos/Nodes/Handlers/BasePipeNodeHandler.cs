using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.NodeContainer.Nodes;
using Content.Shared.NodeContainer.Nodes.Handlers;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Nodes.Handlers;

public abstract class BasePipeNodeHandler<T> : NodeHandler<T>, IRotatableNodeHandler where T : PipeNode
{
    protected override void Initialize(T node, EntityUid owner)
    {
        base.Initialize(node, owner);

        if (!node.RotationsEnabled)
            return;

        var xform = Transform(node.Owner);
        node.CurrentPipeDirection = node.OriginalPipeDirection.RotatePipeDirection(xform.LocalRotation);
    }

    public bool RotateNode(IRotatableNode node, in MoveEvent ev)
    {
        return RotateNode((T) node, ev);
    }

    protected bool RotateNode(T node, in MoveEvent ev)
    {
        if (node.OriginalPipeDirection == PipeDirection.Fourway)
            return false;

        // update valid pipe direction
        if (!node.RotationsEnabled)
        {
            if (node.CurrentPipeDirection == node.OriginalPipeDirection)
                return false;

            node.CurrentPipeDirection = node.OriginalPipeDirection;
            return true;
        }

        var oldDirection = node.CurrentPipeDirection;
        node.CurrentPipeDirection = node.OriginalPipeDirection.RotatePipeDirection(ev.NewRotation);
        return oldDirection != node.CurrentPipeDirection;
    }

    protected override void OnAnchorStateChanged(T node, bool anchored)
    {
        if (!anchored)
            return;

        // update valid pipe directions
        if (!node.RotationsEnabled)
        {
            node.CurrentPipeDirection = node.OriginalPipeDirection;
            return;
        }

        var xform = Transform(node.Owner);
        node.CurrentPipeDirection = node.OriginalPipeDirection.RotatePipeDirection(xform.LocalRotation);
    }

    protected override IEnumerable<Node> GetReachableNodes(
        T node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (node.AlwaysReachable != null)
        {
            var remQ = new RemQueue<PipeNode>();
            foreach (var pipe in node.AlwaysReachable)
            {
                if (pipe.Deleting)
                {
                    remQ.Add(pipe);
                }
                yield return pipe;
            }

            foreach (var pipe in remQ)
            {
                node.AlwaysReachable.Remove(pipe);
            }
        }

        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var pos = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
        {
            var pipeDir = (PipeDirection) (1 << i);

            if (!node.CurrentPipeDirection.HasDirection(pipeDir))
                continue;

            foreach (var pipe in LinkableNodesInDirection(node, pos, pipeDir, gridEnt))
            {
                yield return pipe;
            }
        }
    }

    protected override bool Connectable(T node)
    {
        return node.ConnectionsEnabled && base.Connectable(node);
    }

    /// <summary>
    ///     Gets the pipes that can connect to us from entities on the tile or adjacent in a direction.
    /// </summary>
    private IEnumerable<PipeNode> LinkableNodesInDirection(
        T node,
        Vector2i pos,
        PipeDirection pipeDir,
        Entity<MapGridComponent> grid)
    {
        foreach (var pipe in PipesInDirection(pos, pipeDir, grid))
        {
            if (pipe.NodeGroupID == node.NodeGroupID
                && pipe.CurrentPipeLayer == node.CurrentPipeLayer
                && pipe.CurrentPipeDirection.HasDirection(pipeDir.GetOpposite()))
            {
                yield return pipe;
            }
        }
    }

    /// <summary>
    ///     Gets the pipes from entities on the tile adjacent in a direction.
    /// </summary>
    protected IEnumerable<PipeNode> PipesInDirection(
        Vector2i pos,
        PipeDirection pipeDir,
        Entity<MapGridComponent> grid)
    {
        var offsetPos = pos.Offset(pipeDir.ToDirection());

        foreach (var entity in MapSystem.GetAnchoredEntities(grid, offsetPos))
        {
            if (!NodeQuery.TryGetComponent(entity, out var container))
                continue;

            foreach (var node in container.Nodes.Values)
            {
                if (node is PipeNode pipe)
                    yield return pipe;
            }
        }
    }

    public void AddAlwaysReachable(T node, PipeNode other)
    {
        if (other.NodeGroupID != node.NodeGroupID)
            return;

        node.AlwaysReachable ??= new();
        node.AlwaysReachable.Add(other);

        if (node.NodeGroup != null)
            NodeGroupSys.QueueRemakeGroup((BaseNodeGroup) node.NodeGroup);
    }

    public void RemoveAlwaysReachable(T node, PipeNode other)
    {
        node.AlwaysReachable?.Remove(other);

        if (node.NodeGroup != null)
            NodeGroupSys.QueueRemakeGroup((BaseNodeGroup) node.NodeGroup);
    }
}
