using System.Diagnostics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Content.Shared.Spreader;
using Content.Shared.Tag;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Spreader;

/// <summary>
/// Handles generic spreading logic, where one anchored entity spreads to neighboring tiles.
/// </summary>
public sealed partial class SpreaderSystem : EntitySystem
{
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TurfSystem _turf = default!;
    
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<EdgeSpreaderComponent> _edgeSpreaderQuery = default!;
    [Dependency] private EntityQuery<AirtightComponent> _airtightQuery = default!;
    [Dependency] private EntityQuery<DockingComponent> _dockingQuery = default!;

    /// <summary>
    /// Cached maximum number of updates per interval per spreader prototype. This is applied per-grid.
    /// </summary>
    private Dictionary<string, int> _prototypeUpdates = default!;

    /// <summary>
    /// cached durations of the update interval for each prototype.
    /// </summary>
    private Dictionary<string, float> _prototypeIntervals = default!;

    /// <summary>
    /// Remaining number of updates per grid & prototype.
    /// TODO PERFORMANCE Assign each prototype to an index and convert dictionary to array
    /// </summary>
    private readonly Dictionary<EntityUid, Dictionary<string, int>> _gridUpdates = [];

    public const float SpreadCooldownSeconds = 1;

    private static readonly ProtoId<TagPrototype> IgnoredTag = "SpreaderIgnore";

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
        _prototypeUpdates = [];
        _prototypeIntervals = [];
        foreach (var proto in ProtoMan.EnumeratePrototypes<EdgeSpreaderPrototype>())
        {
            _prototypeUpdates.Add(proto.ID, proto.UpdatesPerInterval);
            _prototypeIntervals.Add(proto.ID, proto.SpreadInterval);
        }
    }

    private void OnAirtightChanged(ref AirtightChanged ev)
    {
        ActivateSpreadableNeighbors(ev.Entity, ev.Position);
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        EnsureComp<SpreaderGridComponent>(ev.EntityUid);
    }

    private void OnTerminating(Entity<EdgeSpreaderComponent> entity, ref EntityTerminatingEvent args)
    {
        ActivateSpreadableNeighbors(entity);
    }
    

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveEdgeSpreaderComponent>();
        var spreaders = new List<(EntityUid Uid, ActiveEdgeSpreaderComponent Comp)>(Count<ActiveEdgeSpreaderComponent>());

        // Build a list of all existing Edgespreaders, shuffle them
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextSpreadTime)
                continue;
            spreaders.Add((uid, comp));
        }

        if (spreaders.Count == 0) 
            return;

        _robustRandom.Shuffle(spreaders);
        _gridUpdates.Clear();
        
        // Remove the EdgeSpreaderComponent from any entity
        // that doesn't meet a few trivial prerequisites
        foreach (var (uid, comp) in spreaders)
        {
            // Get xform first, as entity may have been deleted due to interactions triggered by other spreaders.
            if (!TryComp(uid, out TransformComponent? xform))
                continue;

            if (xform.GridUid == null)
            {
                RemComp(uid, comp);
                continue;
            }

            if (!_gridUpdates.TryGetValue(xform.GridUid.Value, out var groupUpdates))
            {
                groupUpdates = _prototypeUpdates.ShallowClone();
                _gridUpdates[xform.GridUid.Value] = groupUpdates;
            }

            if (!_edgeSpreaderQuery.TryGetComponent(uid, out var spreader))
            {
                RemComp(uid, comp);
                continue;
            }

            if (!groupUpdates.TryGetValue(spreader.Id, out var updates) || updates < 1)
                continue;
            
            var nextSpread = comp.NextSpreadTime + TimeSpan.FromSeconds(_prototypeIntervals[spreader.Id]);

            comp.NextSpreadTime = nextSpread;
            Debug.Assert(nextSpread == comp.NextSpreadTime);
            // Edge detection logic is to be handled
            // by the subscribing system, see KudzuSystem
            // for a simple example
            Spread(uid, xform, spreader.Id, ref updates, comp.NextSpreadTime);
            

            if (updates < 1)
                groupUpdates.Remove(spreader.Id);
            else
                groupUpdates[spreader.Id] = updates;
        }
    }

    /// <summary>
    /// Spreads the edge spreader
    /// </summary>
    /// <param name="uid">uid of the origin entity</param>
    /// <param name="xform">xform of the origin entity</param>
    /// <param name="prototype">ID of the EdgeSpreaderPrototype of the thing being spread</param>
    /// <param name="updates">updates remaining for the edgespreader</param>
    private void Spread(EntityUid uid, TransformComponent xform, ProtoId<EdgeSpreaderPrototype> prototype, ref int updates, TimeSpan nextSpreadTime)
    {
        GetNeighbors(uid, xform, prototype, out var freeTiles, out _, out var neighbors);

        var ev = new SpreadNeighborsEvent()
        {
            NeighborFreeTiles = freeTiles,
            Neighbors = neighbors,
            Updates = updates,
            NextSpreadTime = nextSpreadTime
        };
        RaiseLocalEvent(uid, ref ev);
        updates = ev.Updates;
    }

    /// <summary>
    /// Gets the neighboring node data for the specified entity and the specified node group.
    /// </summary>
    public void GetNeighbors(EntityUid uid, TransformComponent comp, ProtoId<EdgeSpreaderPrototype> prototype, out ValueList<(MapGridComponent, TileRef)> freeTiles, out ValueList<Vector2i> occupiedTiles, out ValueList<EntityUid> neighbors)
    {
        freeTiles = [];
        occupiedTiles = [];
        neighbors = [];
        // TODO remove occupiedTiles -- its currently unused and just slows this method down.
        if (!ProtoMan.Resolve(prototype, out var spreaderPrototype))
            return;

        if (!TryComp<MapGridComponent>(comp.GridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(comp.GridUid.Value, grid, comp.Coordinates);
        var blockedAtmosDirs = AtmosDirection.Invalid;

        // Due to docking ports they may not necessarily be opposite directions.
        var neighborTiles = new ValueList<(EntityUid entity, MapGridComponent grid, Vector2i Indices, AtmosDirection OtherDir, AtmosDirection OurDir)>();

        // Check if anything on our own tile blocking that direction.
        var ourEnts = _map.GetAnchoredEntities(comp.GridUid.Value, grid, tile);

        while (ourEnts.MoveNext(out var ent))
        {
            // Spread via docks in a special-case.
            if (_dockingQuery.TryGetComponent(ent, out var dock) &&
                dock.Docked &&
                TryComp(ent, out TransformComponent? xform) &&
                TryComp(dock.DockedWith, out TransformComponent? dockedXform) &&
                TryComp<MapGridComponent>(dockedXform.GridUid, out var dockedGrid))
            {
                neighborTiles.Add((
                    dockedXform.GridUid.Value, dockedGrid,
                    _map.CoordinatesToTile(dockedXform.GridUid.Value,
                        dockedGrid,
                        dockedXform.Coordinates),
                    xform.LocalRotation.ToAtmosDirection(),
                    dockedXform.LocalRotation.ToAtmosDirection()));
            }

            // If we're on a blocked tile work out which directions we can go.
            if (!_airtightQuery.TryGetComponent(ent, out var airtight) || !airtight.AirBlocked ||
                _tag.HasTag(ent.Value, IgnoredTag))
            {
                continue;
            }

            foreach (var value in new[] { AtmosDirection.North, AtmosDirection.East, AtmosDirection.South, AtmosDirection.West })
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
            var atmosDir = (AtmosDirection)(1 << i);
            var neighborPos = tile.Offset(atmosDir);
            neighborTiles.Add((comp.GridUid.Value, grid, neighborPos, atmosDir, i.ToOppositeDir()));
        }

        foreach (var (neighborEnt, neighborGrid, neighborPos, ourAtmosDir, otherAtmosDir) in neighborTiles)
        {
            // This tile is blocked to that direction.
            if ((blockedAtmosDirs & ourAtmosDir) != 0x0)
                continue;

            if (!_map.TryGetTileRef(neighborEnt, neighborGrid, neighborPos, out var tileRef) || tileRef.Tile.IsEmpty)
                continue;

            if (spreaderPrototype.PreventSpreadOnSpaced && _turf.IsSpace(tileRef))
                continue;

            var directionEnumerator = _map.GetAnchoredEntities(neighborEnt, neighborGrid, neighborPos);
            var occupied = false;

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (!_airtightQuery.TryGetComponent(ent, out var airtight) || !airtight.AirBlocked || _tag.HasTag(ent.Value, IgnoredTag))
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
            directionEnumerator = _map.GetAnchoredEntities(neighborEnt, neighborGrid, neighborPos);

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (!_edgeSpreaderQuery.TryGetComponent(ent, out var spreader))
                    continue;

                if (spreader.Id != prototype)
                    continue;

                neighbors.Add(ent.Value);
                occupiedTiles.Add(neighborPos);
                break;
            }

            if (oldCount == occupiedTiles.Count)
                freeTiles.Add((neighborGrid, tileRef));
        }
    }

    /// <summary>
    /// This function activates all spreaders that are adjacent to a given entity. This also activates other spreaders
    /// on the same tile as the current entity (for thin airtight entities like windoors).
    /// </summary>
    /// <param name="origin">UID of EdgeSpreaderComponent when position is null. Otherwise, UID + pos of AirtightChanged.Entity</param>
    /// <param name="position">Position arg (UID + pos) of source <see cref="Server.Atmos.EntitySystems.AirtightChanged">AirtightChanged</see></param>
    public void ActivateSpreadableNeighbors(EntityUid origin, (EntityUid Grid, Vector2i Tile)? position = null)
    {
        Vector2i tile;
        EntityUid gridUid;
        MapGridComponent? gridComp;

        if (position == null)
        {
            var transform = Transform(origin);
            if (!TryComp(transform.GridUid, out gridComp) || TerminatingOrDeleted(transform.GridUid.Value))
                return;

            tile = _map.TileIndicesFor(transform.GridUid.Value, gridComp, transform.Coordinates);
            gridUid = transform.GridUid.Value;
        }
        else
        {
            if (!TryComp(position.Value.Grid, out gridComp))
                return;
            (gridUid, tile) = position.Value;
        }
        

        var anchored = _map.GetAnchoredEntities(gridUid, gridComp, tile);
        while (anchored.MoveNext(out var entity))
        {
            // Don't re-activate the terminating entity
            if (entity == origin)
                continue;
            DebugTools.Assert(Transform(entity.Value).Anchored);

            // Activate any edge spreaders that are non-terminating
            if (_edgeSpreaderQuery.TryGetComponent(entity, out var spreader) && !TerminatingOrDeleted(entity))
            {
                var nextEmission = _timing.CurTime + TimeSpan.FromSeconds(ProtoMan.Index<EdgeSpreaderPrototype>(spreader.Id).SpreadInterval); //TimeSpan.FromSeconds(_prototypeIntervals[spreader.Id]);
                var activeSpreader = EnsureComp<ActiveEdgeSpreaderComponent>(entity.Value);
                activeSpreader.NextSpreadTime = nextEmission;
                //Log.Debug(nextEmission.ToString());
                DebugTools.AssertEqual(activeSpreader.NextSpreadTime, nextEmission);
            }
        }

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            var adjacentTile = SharedMapSystem.GetDirection(tile, direction.ToDirection());
            anchored = _map.GetAnchoredEntities(gridUid, gridComp, adjacentTile);

            while (anchored.MoveNext(out var entity))
            {
                DebugTools.Assert(Transform(entity.Value).Anchored);

                // Activate any edge spreaders that are non-terminating
                if (_edgeSpreaderQuery.TryGetComponent(entity, out var spreader) && !TerminatingOrDeleted(entity))
                {
                    var nextEmission = _timing.CurTime + TimeSpan.FromSeconds(ProtoMan.Index<EdgeSpreaderPrototype>(spreader.Id).SpreadInterval);
                
                    var activeSpreader = EnsureComp<ActiveEdgeSpreaderComponent>(entity.Value);
                    activeSpreader.NextSpreadTime = nextEmission;
                    
                    //Log.Debug(nextEmission.ToString());
                    DebugTools.AssertEqual(activeSpreader.NextSpreadTime, nextEmission);
                }
            }
        }
    }

    public bool RequiresFloorToSpread(EntProtoId<EdgeSpreaderComponent> spreader)
    {
        if (!ProtoMan.Index(spreader).TryComp<EdgeSpreaderComponent>(out var spreaderComp, EntityManager.ComponentFactory))
            return false;

        return ProtoMan.Index(spreaderComp.Id).PreventSpreadOnSpaced;
    }
}
