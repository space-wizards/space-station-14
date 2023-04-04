using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Physics;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
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
    [DataField("name")]
    public string Name = string.Empty;
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

            var comp = _entManager.EnsureComponent<EdgeSpreaderComponent>(neighborNode.Owner);
            comp.Name = node.Name;
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
            var comp = _entManager.EnsureComponent<EdgeSpreaderComponent>(node.Owner);
            comp.Name = node.Name;
        }
    }
}

public sealed class SpreaderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    private static readonly TimeSpan SpreadCooldown = TimeSpan.FromSeconds(1);

    private static readonly List<string> SpreaderGroups = new()
    {
        "puddle"
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<SpreaderGridComponent, EntityUnpausedEvent>(OnGridUnpaused);
    }

    private void OnAirtightChanged(ref AirtightChanged ev)
    {
        var neighbors = GetNeighbors(ev.Entity);

        foreach (var neighbor in neighbors)
        {
            EnsureComp<EdgeSpreaderComponent>(neighbor);
            // TODO: Name
        }
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
                !nodeComponent.TryGetNode<SpreaderNode>(comp.Name, out var node))
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

            // While we could check if it's an edge here the subscribing system may have its own definition
            // of an edge so we'll let them handle it.
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
        var neighbor = GetNeighbors(uid);

        var ev = new SpreadNeighborsEvent()
        {
            Neighbors = neighbor,
        };
        RaiseLocalEvent(uid, ref ev);
        return !ev.Handled;
    }

    private List<EntityUid> GetNeighbors(EntityUid uid)
    {
        var neighbors = new List<EntityUid>();

        if (!EntityManager.TryGetComponent<TransformComponent>(uid, out var transform))
            return neighbors; // how did we get here?

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
            return neighbors;

        var tile = grid.TileIndicesFor(transform.Coordinates);
        var nodeQuery = GetEntityQuery<NodeContainerComponent>();

        for (var i = 0; i < 4; i++)
        {
            var direction = (Direction) (1 << i);

            // Check if we can spread to that tile.
            var dstMap = dstPos.ToMap(EntityManager, _transform);
            var dst = dstMap.Position;
            var src = Transform(srcUid).MapPosition.Position;
            var dir = src - dst;
            var ray = new CollisionRay(dst, dir.Normalized, (int) (CollisionGroup.Impassable | CollisionGroup.HighImpassable));
            var mapId = dstMap.MapId;
            var results = _physics.IntersectRay(mapId, ray, dir.Length, returnOnFirstHit: true);
            if (results.Any())
            {
                newPuddleUid = null;
                newPuddleComp = null;
                return false;
            }

            var directionEnumerator =
                grid.GetAnchoredEntitiesEnumerator(SharedMapSystem.GetDirection(tile, direction.ToDirection()));

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (!nodeQuery.TryGetComponent(ent, out var nodeContainer))
                    continue;

                foreach (var name in SpreaderGroups)
                {
                    if (!nodeContainer.Nodes.ContainsKey(name))
                        continue;

                    neighbors.Add(ent.Value);
                    break;
                }
            }
        }

        return neighbors;
    }

    private List<EntityUid> GetNeighbors(EntityUid uid, AirtightComponent comp)
    {
        var neighbors = new List<EntityUid>();

        if (!EntityManager.TryGetComponent<TransformComponent>(uid, out var transform))
            return neighbors; // how did we get here?

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
            return neighbors;

        var tile = grid.TileIndicesFor(transform.Coordinates);
        var nodeQuery = GetEntityQuery<NodeContainerComponent>();

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            if (!comp.AirBlockedDirection.IsFlagSet(direction))
                continue;

            var directionEnumerator =
                grid.GetAnchoredEntitiesEnumerator(SharedMapSystem.GetDirection(tile, direction.ToDirection()));

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (!nodeQuery.TryGetComponent(ent, out var nodeContainer))
                    continue;

                foreach (var name in SpreaderGroups)
                {
                    if (!nodeContainer.Nodes.ContainsKey(name))
                        continue;

                    neighbors.Add(ent.Value);
                    break;
                }
            }
        }

        return neighbors;
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
/// If the spread is no longer able to happen you MUST cancel this event!
/// </summary>
[ByRefEvent]
public record struct SpreadNeighborsEvent
{
    public List<EntityUid> Neighbors;

    /// <summary>
    /// Set to true if an update is counted for the node group.
    /// If you wish to stop edge spreading from this entity then remove its component.
    /// </summary>
    public bool Handled;
}
