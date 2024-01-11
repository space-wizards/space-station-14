using Content.Server.Administration.Logs;
using Content.Server.Station.Systems;
using Content.Server.Warps;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Pinpointer;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
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
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly MapSystem _map = default!;

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
        SubscribeLocalEvent<GridSplitEvent>(OnNavMapSplit);

        SubscribeLocalEvent<NavMapBeaconComponent, ComponentStartup>(OnNavMapBeaconStartup);
        SubscribeLocalEvent<NavMapBeaconComponent, AnchorStateChangedEvent>(OnNavMapBeaconAnchor);

        SubscribeLocalEvent<NavMapDoorComponent, ComponentStartup>(OnNavMapDoorStartup);
        SubscribeLocalEvent<NavMapDoorComponent, AnchorStateChangedEvent>(OnNavMapDoorAnchor);

        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, NavMapBeaconConfigureBuiMessage>(OnConfigureMessage);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, MapInitEvent>(OnConfigurableMapInit);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, ExaminedEvent>(OnConfigurableExamined);
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
        UpdateBeaconEnabledVisuals((uid, component));
        RefreshNavGrid(uid);
    }

    private void OnNavMapDoorStartup(Entity<NavMapDoorComponent> ent, ref ComponentStartup args)
    {
        RefreshNavGrid(ent);
    }

    private void OnNavMapDoorAnchor(Entity<NavMapDoorComponent> ent, ref AnchorStateChangedEvent args)
    {
        RefreshNavGrid(ent);
    }

    private void OnConfigureMessage(Entity<ConfigurableNavMapBeaconComponent> ent, ref NavMapBeaconConfigureBuiMessage args)
    {
        if (args.Session.AttachedEntity is not { } user)
            return;

        if (!TryComp<NavMapBeaconComponent>(ent, out var navMap))
            return;

        if (navMap.Text == args.Text &&
            navMap.Color == args.Color &&
            navMap.Enabled == args.Enabled)
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} configured NavMapBeacon \'{ToPrettyString(ent):entity}\' with text \'{args.Text}\', color {args.Color.ToHexNoAlpha()}, and {(args.Enabled ? "enabled" : "disabled")} it.");

        if (TryComp<WarpPointComponent>(ent, out var warpPoint))
        {
            warpPoint.Location = args.Text;
        }

        navMap.Text = args.Text;
        navMap.Color = args.Color;
        navMap.Enabled = args.Enabled;
        UpdateBeaconEnabledVisuals((ent, navMap));
        Dirty(ent, navMap);
        RefreshNavGrid(ent);
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

    private void UpdateBeaconEnabledVisuals(Entity<NavMapBeaconComponent> ent)
    {
        _appearance.SetData(ent, NavMapBeaconVisuals.Enabled, ent.Comp.Enabled && Transform(ent).Anchored);
    }

    /// <summary>
    /// Refreshes the grid for the corresponding beacon.
    /// </summary>
    /// <param name="uid"></param>
    private void RefreshNavGrid(EntityUid uid)
    {
        var xform = Transform(uid);

        if (!TryComp<NavMapComponent>(xform.GridUid, out var navMap))
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

    private void OnNavMapSplit(ref GridSplitEvent args)
    {
        if (!TryComp(args.Grid, out NavMapComponent? comp))
            return;

        var gridQuery = GetEntityQuery<MapGridComponent>();

        foreach (var grid in args.NewGrids)
        {
            var newComp = EnsureComp<NavMapComponent>(grid);
            RefreshGrid(newComp, gridQuery.GetComponent(grid));
        }

        RefreshGrid(comp, gridQuery.GetComponent(args.Grid));
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
        if (!TryComp<MapGridComponent>(uid, out var mapGrid))
            return;

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

        var airlockQuery = EntityQueryEnumerator<NavMapDoorComponent, TransformComponent>();
        var airlocks = new List<NavMapAirlock>();
        while (airlockQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid != uid || !xform.Anchored)
                continue;

            var pos = _map.TileIndicesFor(uid, mapGrid, xform.Coordinates);
            var enumerator = _map.GetAnchoredEntitiesEnumerator(uid, mapGrid, pos);

            var wallPresent = false;
            while (enumerator.MoveNext(out var ent))
            {
                if (!_physicsQuery.TryGetComponent(ent, out var body) ||
                    !body.CanCollide ||
                    !body.Hard ||
                    body.BodyType != BodyType.Static ||
                    !_tags.HasTag(ent.Value, "Wall", _tagQuery) &&
                    !_tags.HasTag(ent.Value, "Window", _tagQuery))
                {
                    continue;
                }

                wallPresent = true;
                break;
            }

            if (wallPresent)
                continue;

            airlocks.Add(new NavMapAirlock(xform.LocalPosition));
        }

        // TODO: Diffs
        args.State = new NavMapComponentState()
        {
            TileData = data,
            Beacons = beacons,
            Airlocks = airlocks
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
                !_tags.HasTag(ent.Value, "Wall", _tagQuery) &&
                !_tags.HasTag(ent.Value, "Window", _tagQuery))
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
        UpdateBeaconEnabledVisuals((uid, comp));
        Dirty(uid, comp);

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
