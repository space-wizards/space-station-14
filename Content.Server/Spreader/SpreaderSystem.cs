using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Spreader;
using Content.Shared.Tag;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Spreader;

/// <summary>
/// Handles generic spreading logic, where one anchored entity spreads to neighboring tiles.
/// </summary>
public sealed class SpreaderSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    /// <summary>
    /// Cached maximum number of updates per spreader prototype. This is applied per-grid.
    /// </summary>
    private Dictionary<string, int> _prototypeUpdates = default!;

    /// <summary>
    /// Remaining number of updates per grid & prototype.
    /// </summary>
    // TODO PERFORMANCE Assign each prototype to an index and convert dictionary to array
    private Dictionary<EntityUid, Dictionary<string, int>> _gridUpdates = new();

    public const float SpreadCooldownSeconds = 1;

    [ValidatePrototypeId<TagPrototype>]
    private const string IgnoredTag = "SpreaderIgnore";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);

        SubscribeLocalEvent<EdgeSpreaderComponent, EntityTerminatingEvent>(OnTerminating);
        SetupPrototypes();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<EdgeSpreaderPrototype>())
            SetupPrototypes();
    }

    private void SetupPrototypes()
    {
        _prototypeUpdates = new Dictionary<string, int>();
        foreach (var proto in _prototype.EnumeratePrototypes<EdgeSpreaderPrototype>())
        {
            _prototypeUpdates.Add(proto.ID, proto.UpdatesPerSecond);
        }
    }

    private void OnAirtightChanged(ref AirtightChanged ev)
    {
        var neighbors = GetSpreadableNeighbors(ev.Entity, ev.Airtight, ev.Position);

        foreach (var neighbor in neighbors)
        {
            EnsureComp<ActiveEdgeSpreaderComponent>(neighbor);
        }
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        EnsureComp<SpreaderGridComponent>(ev.EntityUid);
    }

    private void OnTerminating(EntityUid uid, EdgeSpreaderComponent component, ref EntityTerminatingEvent args)
    {
        var neighbors = GetSpreadableNeighbors(uid);

        foreach (var neighbor in neighbors)
        {
            if (!TerminatingOrDeleted(neighbor))
                EnsureComp<ActiveEdgeSpreaderComponent>(neighbor);
        }
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        // Check which grids are valid for spreading
        var spreadGrids = EntityQueryEnumerator<SpreaderGridComponent>();

        _gridUpdates.Clear();
        while (spreadGrids.MoveNext(out var uid, out var grid))
        {
            grid.UpdateAccumulator -= frameTime;
            if (grid.UpdateAccumulator > 0)
                continue;

            _gridUpdates[uid] = _prototypeUpdates.ShallowClone();
            grid.UpdateAccumulator += SpreadCooldownSeconds;
        }

        if (_gridUpdates.Count == 0)
            return;

        var query = EntityQueryEnumerator<ActiveEdgeSpreaderComponent>();
        var xforms = GetEntityQuery<TransformComponent>();
        var spreaderQuery = GetEntityQuery<EdgeSpreaderComponent>();

        var spreaders = new List<(EntityUid Uid, ActiveEdgeSpreaderComponent Comp)>(Count<ActiveEdgeSpreaderComponent>());

        // Build a list of all existing Edgespreaders, shuffle them
        while (query.MoveNext(out var uid, out var comp))
        {
            spreaders.Add((uid, comp));
        }

        _robustRandom.Shuffle(spreaders);

        // Remove the EdgeSpreaderComponent from any entity
        // that doesn't meet a few trivial prerequisites
        foreach (var (uid, comp) in spreaders)
        {
            // Get xform first, as entity may have been deleted due to interactions triggered by other spreaders.
            if (!xforms.TryGetComponent(uid, out var xform))
                continue;

            if (xform.GridUid == null)
            {
                RemComp(uid, comp);
                continue;
            }

            if (!_gridUpdates.TryGetValue(xform.GridUid.Value, out var groupUpdates))
                continue;

            if (!spreaderQuery.TryGetComponent(uid, out var spreader))
            {
                RemComp(uid, comp);
                continue;
            }

            if (!groupUpdates.TryGetValue(spreader.Id, out var updates) || updates < 1)
                continue;

            // Edge detection logic is to be handled
            // by the subscribing system, see KudzuSystem
            // for a simple example
            Spread(uid, xform, spreader.Id, ref updates);

            if (updates < 1)
                groupUpdates.Remove(spreader.Id);
            else
                groupUpdates[spreader.Id] = updates;
        }
    }

    private void Spread(EntityUid uid, TransformComponent xform, string prototype, ref int updates)
    {
        GetNeighbors(uid, xform, prototype, out var freeTiles, out _, out var neighbors);

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
    public void GetNeighbors(EntityUid uid, TransformComponent transform, string prototype, out ValueList<(MapGridComponent Grid, Vector2i Tile)> freeTiles, out ValueList<Vector2i> occupiedTiles, out ValueList<EntityUid> neighbors)
    {
        // TODO remove occupiedTiles -- its currently unused and just slows this method down.
        DebugTools.Assert(_prototype.HasIndex<EdgeSpreaderPrototype>(prototype));
        freeTiles = new ValueList<(MapGridComponent Grid, Vector2i Tile)>();
        occupiedTiles = new ValueList<Vector2i>();
        neighbors = new ValueList<EntityUid>();

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
            return;

        var tile = grid.TileIndicesFor(transform.Coordinates);
        var spreaderQuery = GetEntityQuery<EdgeSpreaderComponent>();
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
                if (!spreaderQuery.TryGetComponent(ent, out var spreader))
                    continue;

                if (spreader.Id != prototype)
                    continue;

                neighbors.Add(ent.Value);
                occupiedTiles.Add(neighborPos);
                break;
            }

            if (oldCount == occupiedTiles.Count)
                freeTiles.Add((neighborGrid, neighborPos));
        }
    }

    /// <summary>
    /// Given an entity, this returns a list of all adjacent entities with a <see cref="EdgeSpreaderComponent"/>.
    /// </summary>
    public List<EntityUid> GetSpreadableNeighbors(EntityUid uid, AirtightComponent? comp = null,
        (EntityUid Grid, Vector2i Tile)? position = null)
    {
        Resolve(uid, ref comp, false);
        var neighbors = new List<EntityUid>();

        Vector2i tile;
        MapGridComponent? grid;

        if (position == null)
        {
            var transform = Transform(uid);
            if (!_mapManager.TryGetGrid(transform.GridUid, out grid) || TerminatingOrDeleted(transform.GridUid.Value))
                return neighbors;
            tile = grid.TileIndicesFor(transform.Coordinates);
        }
        else
        {
            if (!_mapManager.TryGetGrid(position.Value.Grid, out grid))
                return neighbors;
            tile = position.Value.Tile;
        }

        var spreaderQuery = GetEntityQuery<EdgeSpreaderComponent>();

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            if (comp != null && !comp.AirBlockedDirection.IsFlagSet(direction))
                continue;

            var directionEnumerator =
                grid.GetAnchoredEntitiesEnumerator(SharedMapSystem.GetDirection(tile, direction.ToDirection()));

            while (directionEnumerator.MoveNext(out var ent))
            {
                DebugTools.Assert(Transform(ent.Value).Anchored);
                if (spreaderQuery.HasComponent(ent) && !TerminatingOrDeleted(ent.Value))
                    neighbors.Add(ent.Value);
            }
        }

        return neighbors;
    }
}
