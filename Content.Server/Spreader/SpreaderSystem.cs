using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Spreader;
using Content.Shared.Tag;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Spreader;

/// <summary>
/// Handles generic spreading logic, where one anchored entity spreads to neighboring tiles.
/// </summary>
public sealed class SpreaderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    private static readonly TimeSpan SpreadCooldown = TimeSpan.FromSeconds(1);

    private readonly List<string> _spreaderGroups = new();

    private const string IgnoredTag = "SpreaderIgnore";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);

        SubscribeLocalEvent<SpreaderGridComponent, EntityUnpausedEvent>(OnGridUnpaused);

        SetupPrototypes();
        _prototype.PrototypesReloaded += OnPrototypeReload;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _prototype.PrototypesReloaded -= OnPrototypeReload;
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.ContainsKey(typeof(EdgeSpreaderPrototype)))
            return;

        SetupPrototypes();
    }

    private void SetupPrototypes()
    {
        _spreaderGroups.Clear();

        foreach (var id in _prototype.EnumeratePrototypes<EdgeSpreaderPrototype>())
        {
            _spreaderGroups.Add(id.ID);
        }
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

    }

    /// <inheritdoc/>
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

            foreach (var sGroup in _spreaderGroups)
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
                    var spreadEv = new SpreadGroupUpdateRate(node.Name);
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
        GetNeighbors(uid, node.Name, out var freeTiles, out _, out var neighbors);

        var ev = new SpreadNeighborsEvent()
        {
            NeighborFreeTiles = freeTiles,
            Neighbors = neighbors,
            Updates = updates,
        };

        RaiseLocalEvent(uid, ref ev);
        updates = ev.Updates;
    }

    /// <summary>
    /// Gets the neighboring node data for the specified entity and the specified node group.
    /// </summary>
    public void GetNeighbors(EntityUid uid, string groupName, out ValueList<(MapGridComponent Grid, Vector2i Tile)> freeTiles, out ValueList<Vector2i> occupiedTiles, out ValueList<EntityUid> neighbors)
    {
        freeTiles = new ValueList<(MapGridComponent Grid, Vector2i Tile)>();
        occupiedTiles = new ValueList<Vector2i>();
        neighbors = new ValueList<EntityUid>();

        if (!EntityManager.TryGetComponent<TransformComponent>(uid, out var transform))
            return;

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
            return;

        var tile = grid.TileIndicesFor(transform.Coordinates);
        var nodeQuery = GetEntityQuery<NodeContainerComponent>();
        var airtightQuery = GetEntityQuery<AirtightComponent>();
        var dockQuery = GetEntityQuery<DockingComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var tagQuery = GetEntityQuery<TagComponent>();
        var blockedAtmosDirs = AtmosDirection.Invalid;

        // Due to docking ports they may not necessarily be opposite directions.
        var neighborTiles = new ValueList<(MapGridComponent grid, Vector2i Indices, AtmosDirection OtherDir, AtmosDirection OurDir)>();

        // Check if anything on our own tile blocking that direction.
        var ourEnts = grid.GetAnchoredEntitiesEnumerator(tile);

        while (ourEnts.MoveNext(out var ent))
        {
            // Spread via docks in a special-case.
            if (dockQuery.TryGetComponent(ent, out var dock) &&
                dock.Docked &&
                xformQuery.TryGetComponent(ent, out var xform) &&
                xformQuery.TryGetComponent(dock.DockedWith, out var dockedXform) &&
                TryComp<MapGridComponent>(dockedXform.GridUid, out var dockedGrid))
            {
                neighborTiles.Add((dockedGrid, dockedGrid.CoordinatesToTile(dockedXform.Coordinates), xform.LocalRotation.ToAtmosDirection(), dockedXform.LocalRotation.ToAtmosDirection()));
            }

            // If we're on a blocked tile work out which directions we can go.
            if (!airtightQuery.TryGetComponent(ent, out var airtight) || !airtight.AirBlocked ||
                tagQuery.TryGetComponent(ent, out var tags) && tags.Tags.Contains(IgnoredTag))
            {
                continue;
            }

            foreach (var value in new[] { AtmosDirection.North, AtmosDirection.East, AtmosDirection.South, AtmosDirection.West})
            {
                if ((value & airtight.AirBlockedDirection) == 0x0)
                    continue;

                blockedAtmosDirs |= value;
                break;
            }
            break;
        }

        // Add the normal neighbors.
        for (var i = 0; i < 4; i++)
        {
            var direction = (Direction) (i * 2);
            var atmosDir = direction.ToAtmosDirection();
            var neighborPos = SharedMapSystem.GetDirection(tile, direction);
            neighborTiles.Add((grid, neighborPos, atmosDir, atmosDir.GetOpposite()));
        }

        foreach (var (neighborGrid, neighborPos, ourAtmosDir, otherAtmosDir) in neighborTiles)
        {
            // This tile is blocked to that direction.
            if ((blockedAtmosDirs & ourAtmosDir) != 0x0)
                continue;

            if (!neighborGrid.TryGetTileRef(neighborPos, out var tileRef) || tileRef.Tile.IsEmpty)
                continue;

            var directionEnumerator =
                neighborGrid.GetAnchoredEntitiesEnumerator(neighborPos);
            var occupied = false;

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (!airtightQuery.TryGetComponent(ent, out var airtight) || !airtight.AirBlocked ||
                    tagQuery.TryGetComponent(ent, out var tags) && tags.Tags.Contains(IgnoredTag))
                {
                    continue;
                }

                if ((airtight.AirBlockedDirection & otherAtmosDir) == 0x0)
                    continue;

                occupied = true;
                break;
            }

            if (occupied)
                continue;

            var oldCount = occupiedTiles.Count;
            directionEnumerator =
                neighborGrid.GetAnchoredEntitiesEnumerator(neighborPos);

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
                freeTiles.Add((neighborGrid, neighborPos));
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

                foreach (var name in _spreaderGroups)
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
