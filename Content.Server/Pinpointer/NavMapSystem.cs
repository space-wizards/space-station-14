using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Station.Systems;
using Content.Server.Warps;
using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Maps;
using Content.Shared.Pinpointer;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Pinpointer;

/// <summary>
/// Handles data to be used for in-grid map displays.
/// </summary>
public sealed partial class NavMapSystem : SharedNavMapSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public const float CloseDistance = 15f;
    public const float FarDistance = 30f;

    public override void Initialize()
    {
        base.Initialize();

        // Initialization events
        SubscribeLocalEvent<StationGridAddedEvent>(OnStationInit);

        // Grid change events
        SubscribeLocalEvent<GridSplitEvent>(OnNavMapSplit);
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<AnchorStateChangedEvent>(OnAnchorStateChanged);

        // Beacon events
        SubscribeLocalEvent<NavMapBeaconComponent, MapInitEvent>(OnNavMapBeaconMapInit);
        SubscribeLocalEvent<NavMapBeaconComponent, AnchorStateChangedEvent>(OnNavMapBeaconAnchor);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, NavMapBeaconConfigureBuiMessage>(OnConfigureMessage);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, MapInitEvent>(OnConfigurableMapInit);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, ExaminedEvent>(OnConfigurableExamined);

        // Data handling events
        SubscribeLocalEvent<NavMapComponent, ComponentGetState>(OnGetState);
    }

    #region: Initialization event handling
    private void OnStationInit(StationGridAddedEvent ev)
    {
        var comp = EnsureComp<NavMapComponent>(ev.GridId);
        RefreshGrid(ev.GridId, comp, Comp<MapGridComponent>(ev.GridId));
    }

    #endregion

    #region: Grid change event handling

    private void OnNavMapSplit(ref GridSplitEvent args)
    {
        if (!TryComp(args.Grid, out NavMapComponent? comp))
            return;

        var gridQuery = GetEntityQuery<MapGridComponent>();

        foreach (var grid in args.NewGrids)
        {
            var newComp = EnsureComp<NavMapComponent>(grid);
            RefreshGrid(grid, newComp, gridQuery.GetComponent(grid));
        }

        RefreshGrid(args.Grid, comp, gridQuery.GetComponent(args.Grid));
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        if (!TryComp<NavMapComponent>(ev.NewTile.GridUid, out var navMapRegions))
            return;

        var tile = ev.NewTile.GridIndices;
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, ChunkSize);

        if (!navMapRegions.Chunks.TryGetValue((NavMapChunkType.Floor, chunkOrigin), out var chunk))
            chunk = new(chunkOrigin);

        // This could be easily replaced in the future to accommodate diagonal tiles
        if (ev.NewTile.IsSpace())
            chunk = UnsetAllEdgesForChunkTile(chunk, tile);

        else
            chunk = SetAllEdgesForChunkTile(chunk, tile);

        // Update the component on the server side
        navMapRegions.Chunks[(NavMapChunkType.Floor, chunkOrigin)] = chunk;

        // Update the component on the client side
        RaiseNetworkEvent(new NavMapChunkChangedEvent(GetNetEntity(ev.NewTile.GridUid), NavMapChunkType.Floor, chunkOrigin, chunk.TileData));
    }

    private void OnAnchorStateChanged(ref AnchorStateChangedEvent ev)
    {
        var gridUid = ev.Transform.GridUid;

        if (gridUid == null)
            return;

        if (!TryComp<NavMapComponent>(gridUid, out var navMap) ||
            !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        // We are only concerned with airtight entities (walls, doors, etc) changing their anchor state
        if (!HasComp<AirtightComponent>(ev.Entity))
            return;

        // Refresh the affected tile
        var tile = _mapSystem.CoordinatesToTile(gridUid.Value, mapGrid, _transformSystem.GetMapCoordinates(ev.Entity, ev.Transform));
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, ChunkSize);

        RefreshTile(gridUid.Value, navMap, mapGrid, chunkOrigin, tile);

        // Update potentially affected chunks
        foreach (var category in EntityChunkTypes)
        {
            if (!navMap.Chunks.TryGetValue((category, chunkOrigin), out var chunk))
                continue;

            // Update the component on the server side
            navMap.Chunks[(category, chunkOrigin)] = chunk;

            // Update the component on the client side
            RaiseNetworkEvent(new NavMapChunkChangedEvent(GetNetEntity(gridUid.Value), category, chunkOrigin, chunk.TileData));
        }
    }

    #endregion

    #region: Beacon event handling

    private void OnNavMapBeaconMapInit(EntityUid uid, NavMapBeaconComponent component, MapInitEvent args)
    {
        if (component.DefaultText == null || component.Text != null)
            return;

        component.Text = Loc.GetString(component.DefaultText);
        Dirty(uid, component);

        UpdateNavMapBeaconData(uid, component);
    }

    private void OnNavMapBeaconAnchor(EntityUid uid, NavMapBeaconComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateBeaconEnabledVisuals((uid, component));
        UpdateNavMapBeaconData(uid, component);
    }

    private void OnConfigureMessage(Entity<ConfigurableNavMapBeaconComponent> ent, ref NavMapBeaconConfigureBuiMessage args)
    {
        if (args.Session.AttachedEntity is not { } user)
            return;

        if (!TryComp<NavMapBeaconComponent>(ent, out var beacon))
            return;

        if (beacon.Text == args.Text &&
            beacon.Color == args.Color &&
            beacon.Enabled == args.Enabled)
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} configured NavMapBeacon \'{ToPrettyString(ent):entity}\' with text \'{args.Text}\', color {args.Color.ToHexNoAlpha()}, and {(args.Enabled ? "enabled" : "disabled")} it.");

        if (TryComp<WarpPointComponent>(ent, out var warpPoint))
        {
            warpPoint.Location = args.Text;
        }

        beacon.Text = args.Text;
        beacon.Color = args.Color;
        beacon.Enabled = args.Enabled;

        UpdateBeaconEnabledVisuals((ent, beacon));
        UpdateNavMapBeaconData(ent, beacon);
    }

    private void OnConfigurableMapInit(Entity<ConfigurableNavMapBeaconComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<NavMapBeaconComponent>(ent, out var navMap))
            return;

        // We set this on mapinit just in case the text was edited via VV or something.
        if (TryComp<WarpPointComponent>(ent, out var warpPoint))
        {
            warpPoint.Location = navMap.Text;
        }

        UpdateBeaconEnabledVisuals((ent, navMap));
    }

    private void OnConfigurableExamined(Entity<ConfigurableNavMapBeaconComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<NavMapBeaconComponent>(ent, out var navMap))
            return;

        args.PushMarkup(Loc.GetString("nav-beacon-examine-text",
            ("enabled", navMap.Enabled),
            ("color", navMap.Color.ToHexNoAlpha()),
            ("label", navMap.Text ?? string.Empty)));
    }

    #endregion

    #region: State event handling

    private void OnGetState(EntityUid uid, NavMapComponent component, ref ComponentGetState args)
    {
        // Get the chunk data
        var chunkData = new Dictionary<(NavMapChunkType, Vector2i), Dictionary<AtmosDirection, ushort>>(component.Chunks.Count);

        foreach (var ((category, origin), chunk) in component.Chunks)
        {
            var chunkDatum = new Dictionary<AtmosDirection, ushort>(chunk.TileData.Count);

            foreach (var (direction, tileData) in chunk.TileData)
                chunkDatum[direction] = tileData;

            chunkData.Add((category, origin), chunkDatum);
        }

        // Get the station beacons
        var beacons = new List<NavMapBeacon>();
        var beaconQuery = AllEntityQuery<NavMapBeaconComponent, TransformComponent>();

        while (beaconQuery.MoveNext(out var beaconUid, out var beacon, out var xform))
        {
            if (xform.GridUid != uid)
                continue;

            if (!TryGetNavMapBeaconData(beaconUid, beacon, xform, out var beaconData))
                continue;

            beacons.Add(beaconData.Value);
        }

        // Set the state
        args.State = new NavMapComponentState()
        {
            ChunkData = chunkData,
            Beacons = beacons,
        };
    }

    #endregion

    #region: Grid functions

    private void RefreshGrid(EntityUid uid, NavMapComponent component, MapGridComponent mapGrid)
    {
        var tileRefs = _mapSystem.GetAllTiles(uid, mapGrid);

        foreach (var tileRef in tileRefs)
        {
            var tile = tileRef.GridIndices;
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, ChunkSize);

            if (!component.Chunks.TryGetValue((NavMapChunkType.Floor, chunkOrigin), out var chunk))
                chunk = new(chunkOrigin);

            component.Chunks[(NavMapChunkType.Floor, chunkOrigin)] = SetAllEdgesForChunkTile(chunk, tile);

            // Refresh the contents of the tile
            RefreshTile(uid, component, mapGrid, chunkOrigin, tile);
        }

        Dirty(uid, component);
    }

    private void RefreshTile(EntityUid uid, NavMapComponent component, MapGridComponent mapGrid, Vector2i chunkOrigin, Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);
        var flag = (ushort) GetFlag(relative);
        var invFlag = (ushort) ~flag;

        // Clear stale data from the tile across all entity associated chunks
        foreach (var category in EntityChunkTypes)
        {
            if (!component.Chunks.TryGetValue((category, chunkOrigin), out var chunk))
                chunk = new(chunkOrigin);

            foreach (var (direction, _) in chunk.TileData)
                chunk.TileData[direction] &= invFlag;

            component.Chunks[(category, chunkOrigin)] = chunk;
        }

        // Update the tile data based on what entities are still anchored to the tile
        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(uid, mapGrid, tile);

        while (enumerator.MoveNext(out var ent))
        {
            if (!TryComp<AirtightComponent>(ent, out var entAirtight))
                continue;

            var category = GetAssociatedEntityChunkType(ent.Value);

            if (!component.Chunks.TryGetValue((category, chunkOrigin), out var chunk))
                continue;

            foreach (var (direction, _) in chunk.TileData)
            {
                if ((direction & entAirtight.AirBlockedDirection) > 0)
                    chunk.TileData[direction] |= flag;
            }

            component.Chunks[(category, chunkOrigin)] = chunk;
        }

        // Remove walls that intersect with doors (unless they can both physically fit on the same tile)
        if (component.Chunks.TryGetValue((NavMapChunkType.Wall, chunkOrigin), out var wallChunk) &&
            component.Chunks.TryGetValue((NavMapChunkType.Airlock, chunkOrigin), out var airlockChunk))
        {
            foreach (var (direction, _) in wallChunk.TileData)
            {
                var airlockInvFlag = (ushort) ~airlockChunk.TileData[direction];
                wallChunk.TileData[direction] &= airlockInvFlag;
            }

            component.Chunks[(NavMapChunkType.Wall, chunkOrigin)] = wallChunk;
        }
    }

    #endregion

    #region: Beacon functions

    private bool TryGetNavMapBeaconData(EntityUid uid, NavMapBeaconComponent component, TransformComponent xform, [NotNullWhen(true)] out NavMapBeacon? beaconData)
    {
        beaconData = null;

        if (!component.Enabled || xform.GridUid == null || !xform.Anchored)
            return false;

        // TODO: Make warp points use metadata name instead.
        string? name = component.Text;

        if (string.IsNullOrEmpty(name))
        {
            if (TryComp<WarpPointComponent>(uid, out var warpPoint) && warpPoint.Location != null)
                name = warpPoint.Location;

            else
                name = MetaData(uid).EntityName;
        }

        beaconData = new NavMapBeacon(GetNetEntity(uid), component.Color, name, xform.LocalPosition);

        return true;
    }

    private void UpdateNavMapBeaconData(EntityUid uid, NavMapBeaconComponent component, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return;

        if (xform.GridUid == null)
            return;

        if (!TryGetNavMapBeaconData(uid, component, xform, out var beaconData))
            return;

        RaiseNetworkEvent(new NavMapBeaconChangedEvent(GetNetEntity(xform.GridUid.Value), beaconData.Value));
    }

    private void UpdateBeaconEnabledVisuals(Entity<NavMapBeaconComponent> ent)
    {
        _appearance.SetData(ent, NavMapBeaconVisuals.Enabled, ent.Comp.Enabled && Transform(ent).Anchored);
    }

    /// <summary>
    /// Sets the beacon's Enabled field and refreshes the grid.
    /// </summary>
    public void SetBeaconEnabled(EntityUid uid, bool enabled, NavMapBeaconComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Enabled == enabled)
            return;

        comp.Enabled = enabled;
        UpdateBeaconEnabledVisuals((uid, comp));
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

    /// <summary>
    /// For a given position, tries to find the nearest configurable beacon that is marked as visible.
    /// This is used for things like announcements where you want to find the closest "landmark" to something.
    /// </summary>
    [PublicAPI]
    public bool TryGetNearestBeacon(Entity<TransformComponent?> ent,
        [NotNullWhen(true)] out Entity<NavMapBeaconComponent>? beacon,
        [NotNullWhen(true)] out MapCoordinates? beaconCoords)
    {
        beacon = null;
        beaconCoords = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return TryGetNearestBeacon(_transformSystem.GetMapCoordinates(ent, ent.Comp), out beacon, out beaconCoords);
    }

    /// <summary>
    /// For a given position, tries to find the nearest configurable beacon that is marked as visible.
    /// This is used for things like announcements where you want to find the closest "landmark" to something.
    /// </summary>
    public bool TryGetNearestBeacon(MapCoordinates coordinates,
        [NotNullWhen(true)] out Entity<NavMapBeaconComponent>? beacon,
        [NotNullWhen(true)] out MapCoordinates? beaconCoords)
    {
        beacon = null;
        beaconCoords = null;
        var minDistance = float.PositiveInfinity;

        var query = EntityQueryEnumerator<ConfigurableNavMapBeaconComponent, NavMapBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var navBeacon, out var xform))
        {
            if (!navBeacon.Enabled)
                continue;

            if (navBeacon.Text == null)
                continue;

            if (coordinates.MapId != xform.MapID)
                continue;

            var coords = _transformSystem.GetWorldPosition(xform);
            var distanceSquared = (coordinates.Position - coords).LengthSquared();
            if (!float.IsInfinity(minDistance) && distanceSquared >= minDistance)
                continue;

            minDistance = distanceSquared;
            beacon = (uid, navBeacon);
            beaconCoords = new MapCoordinates(coords, xform.MapID);
        }

        return beacon != null;
    }

    [PublicAPI]
    public string GetNearestBeaconString(Entity<TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return Loc.GetString("nav-beacon-pos-no-beacons");

        return GetNearestBeaconString(_transformSystem.GetMapCoordinates(ent, ent.Comp));
    }

    public string GetNearestBeaconString(MapCoordinates coordinates)
    {
        if (!TryGetNearestBeacon(coordinates, out var beacon, out var pos))
            return Loc.GetString("nav-beacon-pos-no-beacons");

        var gridOffset = Angle.Zero;
        if (_mapManager.TryFindGridAt(pos.Value, out var grid, out _))
            gridOffset = Transform(grid).LocalRotation;

        // get the angle between the two positions, adjusted for the grid rotation so that
        // we properly preserve north in relation to the grid.
        var dir = (pos.Value.Position - coordinates.Position).ToWorldAngle();
        var adjustedDir = (dir - gridOffset).GetDir();

        var length = (pos.Value.Position - coordinates.Position).Length();
        if (length < CloseDistance)
        {
            return Loc.GetString("nav-beacon-pos-format",
                ("color", beacon.Value.Comp.Color),
                ("marker", beacon.Value.Comp.Text!));
        }

        var modifier = length > FarDistance
            ? Loc.GetString("nav-beacon-pos-format-direction-mod-far")
            : string.Empty;

        // we can null suppress the text being null because TryGetNearestVisibleStationBeacon always gives us a beacon with not-null text.
        return Loc.GetString("nav-beacon-pos-format-direction",
            ("modifier", modifier),
            ("direction", ContentLocalizationManager.FormatDirection(adjustedDir).ToLowerInvariant()),
            ("color", beacon.Value.Comp.Color),
            ("marker", beacon.Value.Comp.Text!));
    }

    #endregion
}
