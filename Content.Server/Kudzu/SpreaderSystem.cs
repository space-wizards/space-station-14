using System.Linq;
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
using Robust.Shared.Physics.Systems;
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

[RegisterComponent]
public sealed class EdgeSpreaderComponent : Component
{
}

[NodeGroup(NodeGroupID.Spreader)]
public sealed class SpreaderNodeGroup : BaseNodeGroup
{
    private IEntityManager _entManager = default!;

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);
        _entManager = entMan;
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        foreach (var neighborNode in node.ReachableNodes)
        {
            if (_entManager.Deleted(neighborNode.Owner))
                continue;

            _entManager.EnsureComponent<EdgeSpreaderComponent>(neighborNode.Owner);
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
        "kudzu",
        "puddle",
        "smoke",
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<SpreaderGridComponent, EntityUnpausedEvent>(OnGridUnpaused);
    }

    private void OnAirtightChanged(ref AirtightChanged ev)
    {
        var neighbors = GetNeighbors(ev.Entity, ev.Airtight);

        foreach (var neighbor in neighbors)
        {
            EnsureComp<EdgeSpreaderComponent>(neighbor);
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
                xform.GridUid == null ||
                !gridQuery.HasComponent(xform.GridUid.Value))
            {
                RemCompDeferred<EdgeSpreaderComponent>(uid);
                continue;
            }

            foreach (var sGroup in SpreaderGroups)
            {
                // Cleanup
                if (!nodeQuery.TryGetComponent(uid, out var nodeComponent))
                {
                    RemCompDeferred<EdgeSpreaderComponent>(uid);
                    continue;
                }

                if (!nodeComponent.TryGetNode<SpreaderNode>(sGroup, out var node))
                    continue;

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
                    updates = (int) (spreadEv.UpdatesPerSecond * SpreadCooldown / TimeSpan.FromSeconds(1));
                }

                if (updates <= 0)
                {
                    continue;
                }

                Spread(uid, node, node.NodeGroup, ref updates);
                groupUpdates[node.NodeGroup] = updates;
            }
        }
    }

    private void Spread(EntityUid uid, SpreaderNode node, INodeGroup group, ref int updates)
    {
        GetNeighbors(uid, node.Name, out var freeTiles, out var occupiedTiles, out var neighbors);

        TryComp<MapGridComponent>(Transform(uid).GridUid, out var grid);

        var ev = new SpreadNeighborsEvent()
        {
            Grid = grid,
            NeighborFreeTiles = freeTiles,
            NeighborOccupiedTiles = occupiedTiles,
            Neighbors = neighbors,
            Updates = updates,
        };

        RaiseLocalEvent(uid, ref ev);
        updates = ev.Updates;
    }

    public void GetNeighbors(EntityUid uid, string groupName, out ValueList<Vector2i> freeTiles, out ValueList<Vector2i> occupiedTiles, out ValueList<EntityUid> neighbors)
    {
        freeTiles = new ValueList<Vector2i>();
        occupiedTiles = new ValueList<Vector2i>();
        neighbors = new ValueList<EntityUid>();

        if (!EntityManager.TryGetComponent<TransformComponent>(uid, out var transform))
            return;

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
            return;

        var tile = grid.TileIndicesFor(transform.Coordinates);
        var nodeQuery = GetEntityQuery<NodeContainerComponent>();
        var airtightQuery = GetEntityQuery<AirtightComponent>();

        for (var i = 0; i < 4; i++)
        {
            var direction = (Direction) (i * 2);
            var neighborPos = SharedMapSystem.GetDirection(tile, direction);

            var directionEnumerator =
                grid.GetAnchoredEntitiesEnumerator(neighborPos);
            var occupied = false;

            while (directionEnumerator.MoveNext(out var ent))
            {
                // TODO: Airtight blocked direction for thindows.
                if (airtightQuery.TryGetComponent(ent, out var airtight) && airtight.AirBlocked)
                {
                    // Check if air direction matters.
                    var blocked = false;

                    foreach (var value in new[] { AtmosDirection.North, AtmosDirection.East})
                    {
                        if ((value & airtight.AirBlockedDirection) == 0x0)
                            continue;

                        var airDirection = value.ToDirection();
                        var oppositeDirection = value.ToDirection();

                        if (direction != airDirection && direction != oppositeDirection)
                            continue;

                        blocked = true;
                        break;
                    }

                    if (!blocked)
                        continue;

                    occupied = true;
                    break;
                }
            }

            if (occupied)
                continue;

            var oldCount = occupiedTiles.Count;
            directionEnumerator =
                grid.GetAnchoredEntitiesEnumerator(neighborPos);

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (!nodeQuery.TryGetComponent(ent, out var nodeContainer))
                    continue;

                if (!nodeContainer.Nodes.ContainsKey(groupName))
                    continue;

                neighbors.Add(ent.Value);
                occupiedTiles.Add(neighborPos);
                break;
            }

            if (oldCount == occupiedTiles.Count)
                freeTiles.Add(neighborPos);
        }
    }

    public List<EntityUid> GetNeighbors(EntityUid uid, AirtightComponent comp)
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
    public int UpdatesPerSecond = 16;
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
    public MapGridComponent? Grid;
    public ValueList<Vector2i> NeighborFreeTiles;
    public ValueList<Vector2i> NeighborOccupiedTiles;
    public ValueList<EntityUid> Neighbors;

    /// <summary>
    /// How many updates allowed are remaining.
    /// Subscribers can handle as they wish.
    /// </summary>
    public int Updates;
}

