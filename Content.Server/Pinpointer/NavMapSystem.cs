using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Warps;
using Content.Shared.Pinpointer;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Pinpointer;

/// <summary>
/// Handles data to be used for in-grid map displays.
/// </summary>
public sealed class NavMapSystem : SharedNavMapSystem
{
    [Dependency] private readonly TagSystem _tags = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TagComponent> _tagQuery;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _tagQuery = GetEntityQuery<TagComponent>();

        SubscribeLocalEvent<AnchorStateChangedEvent>(OnAnchorChange);
        SubscribeLocalEvent<ReAnchorEvent>(OnReAnchor);
        SubscribeLocalEvent<StationGridAddedEvent>(OnStationInit);
        SubscribeLocalEvent<NavMapComponent, ComponentStartup>(OnNavMapStartup);
        SubscribeLocalEvent<NavMapComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<NavMapComponent, GridSplitEvent>(OnNavMapSplit);

        SubscribeLocalEvent<NavMapBeaconComponent, ComponentStartup>(OnNavMapBeaconStartup);
        SubscribeLocalEvent<NavMapBeaconComponent, AnchorStateChangedEvent>(OnNavMapBeaconAnchor);
    }

    private void OnStationInit(StationGridAddedEvent ev)
    {
        var comp = EnsureComp<NavMapComponent>(ev.GridId);
        RefreshGrid(comp, Comp<MapGridComponent>(ev.GridId));
    }

    private void OnNavMapBeaconStartup(EntityUid uid, NavMapBeaconComponent component, ComponentStartup args)
    {
        RefreshNavGrid(uid);
    }

    private void OnNavMapBeaconAnchor(EntityUid uid, NavMapBeaconComponent component, ref AnchorStateChangedEvent args)
    {
        RefreshNavGrid(uid);
    }

    /// <summary>
    /// Refreshes the grid for the corresponding beacon.
    /// </summary>
    /// <param name="uid"></param>
    private void RefreshNavGrid(EntityUid uid)
    {
        var xform = Transform(uid);

        if (!CanBeacon(uid, xform) || !TryComp<NavMapComponent>(xform.GridUid, out var navMap))
            return;

        Dirty(xform.GridUid.Value, navMap);
    }

    private bool CanBeacon(EntityUid uid, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return false;

        return xform.GridUid != null && xform.Anchored;
    }

    private void OnNavMapStartup(EntityUid uid, NavMapComponent component, ComponentStartup args)
    {
        if (!TryComp<MapGridComponent>(uid, out var grid))
            return;

        RefreshGrid(component, grid);
    }

    private void OnNavMapSplit(EntityUid uid, NavMapComponent component, ref GridSplitEvent args)
    {
        var gridQuery = GetEntityQuery<MapGridComponent>();

        foreach (var grid in args.NewGrids)
        {
            var newComp = EnsureComp<NavMapComponent>(grid);
            RefreshGrid(newComp, gridQuery.GetComponent(grid));
        }

        RefreshGrid(component, gridQuery.GetComponent(uid));
    }

    private void RefreshGrid(NavMapComponent component, MapGridComponent grid)
    {
        component.Chunks.Clear();

        var tiles = grid.GetAllTilesEnumerator();

        while (tiles.MoveNext(out var tile))
        {
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.Value.GridIndices, ChunkSize);

            if (!component.Chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new NavMapChunk(chunkOrigin);
                component.Chunks[chunkOrigin] = chunk;
            }

            RefreshTile(grid, component, chunk, tile.Value.GridIndices);
        }
    }

    private void OnGetState(EntityUid uid, NavMapComponent component, ref ComponentGetState args)
    {
        var data = new Dictionary<Vector2i, int>(component.Chunks.Count);
        foreach (var (index, chunk) in component.Chunks)
        {
            data.Add(index, chunk.TileData);
        }

        var beaconQuery = AllEntityQuery<NavMapBeaconComponent, TransformComponent>();
        var beacons = new List<NavMapBeacon>();

        while (beaconQuery.MoveNext(out var beaconUid, out var beacon, out var xform))
        {
            if (!beacon.Enabled || xform.GridUid != uid || !CanBeacon(beaconUid, xform))
                continue;

            // TODO: Make warp points use metadata name instead.
            string? name = beacon.Text;

            if (name == null)
            {
                if (TryComp<WarpPointComponent>(beaconUid, out var warpPoint) && warpPoint.Location != null)
                {
                    name = warpPoint.Location;
                }
                else
                {
                    name = MetaData(beaconUid).EntityName;
                }
            }

            beacons.Add(new NavMapBeacon(beacon.Color, name, xform.LocalPosition));
        }

        // TODO: Diffs
        args.State = new NavMapComponentState()
        {
            TileData = data,
            Beacons = beacons,
        };
    }

    private void OnReAnchor(ref ReAnchorEvent ev)
    {
        if (TryComp<MapGridComponent>(ev.OldGrid, out var oldGrid) &&
            TryComp<NavMapComponent>(ev.OldGrid, out var navMap))
        {
            var chunkOrigin = SharedMapSystem.GetChunkIndices(ev.TilePos, ChunkSize);

            if (navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                RefreshTile(oldGrid, navMap, chunk, ev.TilePos);
            }
        }

        HandleAnchor(ev.Xform);
    }

    private void OnAnchorChange(ref AnchorStateChangedEvent ev)
    {
        HandleAnchor(ev.Transform);
    }

    private void HandleAnchor(TransformComponent xform)
    {
        if (!TryComp<NavMapComponent>(xform.GridUid, out var navMap) ||
            !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tile = grid.LocalToTile(xform.Coordinates);
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, ChunkSize);

        if (!navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
        {
            chunk = new NavMapChunk(chunkOrigin);
            navMap.Chunks[chunkOrigin] = chunk;
        }

        RefreshTile(grid, navMap, chunk, tile);
    }

    private void RefreshTile(MapGridComponent grid, NavMapComponent component, NavMapChunk chunk, Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);

        var existing = chunk.TileData;
        var flag = GetFlag(relative);

        chunk.TileData &= ~flag;

        var enumerator = grid.GetAnchoredEntitiesEnumerator(tile);
        // TODO: Use something to get convex poly.

        while (enumerator.MoveNext(out var ent))
        {
            if (!_physicsQuery.TryGetComponent(ent, out var body) ||
                !body.CanCollide ||
                !body.Hard ||
                body.BodyType != BodyType.Static ||
                (!_tags.HasTag(ent.Value, "Wall", _tagQuery) &&
                 !_tags.HasTag(ent.Value, "Window", _tagQuery)))
            {
                continue;
            }

            chunk.TileData |= flag;
            break;
        }

        if (chunk.TileData == 0)
        {
            component.Chunks.Remove(chunk.Origin);
        }

        if (existing == chunk.TileData)
            return;

        Dirty(component);
    }

    /// <summary>
    /// Sets the beacon's Enabled field and refreshes the grid.
    /// </summary>
    public void SetBeaconEnabled(EntityUid uid, bool enabled, NavMapBeaconComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Enabled == enabled)
            return;

        comp.Enabled = enabled;

        RefreshNavGrid(uid);
    }

    /// <summary>
    /// Toggles the beacon's Enabled field and refreshes the grid.
    /// </summary>
    public void ToggleBeacon(EntityUid uid, NavMapBeaconComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        SetBeaconEnabled(uid, !comp.Enabled, comp);
    }
}
