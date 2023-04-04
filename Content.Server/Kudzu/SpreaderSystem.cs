using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Server.Kudzu;

public sealed class SpreaderNode : Node
{
    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform, EntityQuery<NodeContainerComponent> nodeQuery, EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid, IEntityManager entMan)
    {
        if (grid == null)
            yield break;

        var pos = grid.TileIndicesFor(xform.Coordinates);

        for (var i = 0; i < 4; i++)
        {
            var dir = (Direction) (2 * i);
            var neighbor = pos + dir.ToIntVec();

            if (!grid.TryGetTileRef(neighbor, out var tileRef) || tileRef.Tile.IsEmpty)
                continue;

            var anchored = grid.GetAnchoredEntitiesEnumerator(neighbor);

            while (anchored.MoveNext(out var ent))
            {
                if (!nodeQuery.TryGetComponent(ent, out var nodeContainer))
                    continue;

                if (nodeContainer.Nodes.TryGetValue(Name, out var neighborNode))
                {
                    yield return neighborNode;
                }
            }
        }
    }
}

[RegisterComponent]
public sealed class EdgeSpreaderComponent : Component
{

}

[NodeGroup(NodeGroupID.Spreader)]
public sealed class SpreaderNodeGroup : BaseNodeGroup
{
    private IEntityManager _entManager = default!;
    private SpreaderSystem _system = default!;

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);
        _entManager = entMan;
        _system = _entManager.System<SpreaderSystem>();
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var containerQuery = _entManager.GetEntityQuery<NodeContainerComponent>();

        // Iterate neighbors and check if they need to be made spreaders.
        foreach (var neighborNode in node.ReachableNodes)
        {
            if (!_system.IsEdge(neighborNode, containerQuery, xformQuery, node))
                continue;

            _entManager.EnsureComponent<EdgeSpreaderComponent>(neighborNode.Owner);
        }
    }

    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        var containerQuery = _entManager.GetEntityQuery<NodeContainerComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var node in groupNodes)
        {
            if (!_system.IsEdge(node, containerQuery, xformQuery))
                continue;

            // Cleanup isn't super important as worst case we just iterate and realise the
            // neighbors are no longer valid edges.
            _entManager.EnsureComponent<EdgeSpreaderComponent>(node.Owner);
        }
    }
}

public sealed class SpreaderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    // TODO: add spreaders into node groups.
    // Rate-limit spreading per node-group.
    // Copy some fluid stuff across.
    // Copy smokes across.

    private static readonly TimeSpan SpreadCooldown = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<SpreaderGridComponent, EntityUnpausedEvent>(OnGridUnpaused);
    }

    private void OnGridUnpaused(EntityUid uid, SpreaderGridComponent component, ref EntityUnpausedEvent args)
    {
        component.NextUpdate += args.PausedTime;
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        var comp = EnsureComp<SpreaderGridComponent>(ev.EntityUid);
        var nextUpdate = _timing.CurTime;

        // TODO: I believe we need grid mapinit events so we can set the time correctly only on mapinit
        // and not touch it on regular init.
        if (comp.NextUpdate < nextUpdate)
            comp.NextUpdate = nextUpdate;
    }

    private void OnAirtightChanged(ref AirtightChanged ev)
    {
        UpdateNearbySpreaders(ev.Entity, ev.Airtight);
    }

    /// <summary>
    /// Adds adjacent neighbors as edge spreaders.
    /// </summary>
    public void UpdateNearbySpreaders(EntityUid blocker, AirtightComponent comp)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(blocker, out var transform))
            return; // how did we get here?

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid)) return;

        var spreaderQuery = GetEntityQuery<SpreaderComponent>();
        var tile = grid.TileIndicesFor(transform.Coordinates);

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            if (!comp.AirBlockedDirection.IsFlagSet(direction)) continue;

            var directionEnumerator =
                grid.GetAnchoredEntitiesEnumerator(SharedMapSystem.GetDirection(tile, direction.ToDirection()));

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (spreaderQuery.TryGetComponent(ent, out var s) && s.Enabled)
                {
                    EnsureComp<EdgeSpreaderComponent>(ent.Value);
                }
            }
        }
    }

    // TODO: Add IsEdge here and check it.
    public bool IsEdge(Node node, EntityQuery<NodeContainerComponent> containerQuery, EntityQuery<TransformComponent> xformQuery, Node? ignoredNode = null)
    {
        var xform = xformQuery.GetComponent(node.Owner);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        var pos = grid.TileIndicesFor(xform.Coordinates);

        for (var i = 0; i < 4; i++)
        {
            var dir = (Direction) (2 * i);
            var neighbor = pos + dir.ToIntVec();

            if (!grid.TryGetTileRef(neighbor, out var tileRef) || tileRef.Tile.IsEmpty)
                continue;

            var anchored = grid.GetAnchoredEntitiesEnumerator(neighbor);
            var isEdge = true;

            while (anchored.MoveNext(out var ent))
            {
                if (!containerQuery.TryGetComponent(ent, out var nodeContainer))
                    continue;

                if (nodeContainer.Nodes.TryGetValue(node.Name, out var neighborNode) && neighborNode != ignoredNode)
                {
                    isEdge = false;
                    break;
                }
            }

            if (isEdge)
                return true;
        }

        return false;
    }


    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;

        // Check which grids are valid for spreading.
        var spreadable = new ValueList<EntityUid>();
        var spreadGrids = EntityQueryEnumerator<SpreaderGridComponent>();

        while (spreadGrids.MoveNext(out var uid, out var grid))
        {
            if (grid.NextUpdate > curTime)
                continue;

            spreadable.Add(uid);
            grid.NextUpdate += SpreadCooldown;
        }

        if (spreadable.Count == 0)
            return;

        var query = EntityQueryEnumerator<EdgeSpreaderComponent>();
        var nodeQuery = GetEntityQuery<NodeContainerComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var gridQuery = GetEntityQuery<SpreaderGridComponent>();

        // Events and stuff
        var groupUpdates = new Dictionary<INodeGroup, int>();
        var spreaders = new List<(EntityUid Uid, EdgeSpreaderComponent Comp)>(Count<EdgeSpreaderComponent>());

        while (query.MoveNext(out var uid, out var comp))
        {
            spreaders.Add((uid, comp));
        }

        _robustRandom.Shuffle(spreaders);

        foreach (var (uid, comp) in spreaders)
        {
            if (!xformQuery.TryGetComponent(uid, out var xform) ||
                xform.GridUid == null)
            {
                RemCompDeferred<EdgeSpreaderComponent>(uid);
                continue;
            }

            // Cleanup
            if (!gridQuery.HasComponent(xform.GridUid.Value) ||
                !nodeQuery.TryGetComponent(uid, out var nodeComponent) ||
                !nodeComponent.TryGetNode<SpreaderNode>("puddle", out var node))
            {
                RemCompDeferred<EdgeSpreaderComponent>(uid);
                continue;
            }

            // Not allowed this tick?
            if (node.NodeGroup == null ||
                !spreadable.Contains(xform.GridUid.Value))
            {
                continue;
            }

            if (!IsEdge(node, nodeQuery, xformQuery))
            {
                RemCompDeferred<EdgeSpreaderComponent>(uid);
                continue;
            }

            if (!groupUpdates.TryGetValue(node.NodeGroup, out var updates))
            {
                var spreadEv = new SpreadGroupUpdateRate()
                {
                    Name = node.Name,
                };
                RaiseLocalEvent(ref spreadEv);
                updates = spreadEv.Updates;
            }

            if (updates <= 0)
            {
                continue;
            }

            if (!Spread(uid, node, node.NodeGroup))
            {
                continue;
            }

            updates--;
            groupUpdates[node.NodeGroup] = updates;
        }
    }

    private bool Spread(EntityUid uid, SpreaderNode node, INodeGroup group)
    {
        var ev = new SpreadNeighborsEvent();
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }
}

/// <summary>
/// Raised every tick to determine how many updates a particular spreading node group is allowed.
/// </summary>
[ByRefEvent]
public record struct SpreadGroupUpdateRate()
{
    public string Name;
    public int Updates = 3;
}

[RegisterComponent]
public sealed class SpreaderGridComponent : Component
{
    [DataField("nextUpdate", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

/// <summary>
/// Raised when trying to spread to neighboring tiles.
/// </summary>
[ByRefEvent]
public record struct SpreadNeighborsEvent
{
    /// <summary>
    /// Set to true if you wish this node to stop spreading.
    /// </summary>
    public bool Cancelled;
}
