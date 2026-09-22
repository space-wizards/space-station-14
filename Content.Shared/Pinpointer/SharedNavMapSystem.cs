using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Tag;
using Content.Shared.Wall;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Shared.Pinpointer;

public abstract partial class NavMapSystem : EntitySystem
{
    public const int Categories = 3;
    public const int Directions = 4; // Not directly tied to number of atmos directions

    public const int ChunkSize = 8;
    public const int ArraySize = ChunkSize * ChunkSize;

    public const int AllDirMask = (1 << Directions) - 1;
    public const int AirlockMask = AllDirMask << (int) NavMapChunkType.Airlock;
    public const int WallMask = AllDirMask << (int) NavMapChunkType.Wall;
    public const int FloorMask = AllDirMask << (int) NavMapChunkType.Floor;

    public const float CloseDistance = 15f;
    public const float FarDistance = 30f;

    [Dependency] protected SharedMapSystem MapSystem = default!;

    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private EntityQuery<NavMapDoorComponent> _doorQuery;
    [Dependency] private EntityQuery<WallComponent> _wallQuery;

    private static readonly ProtoId<TagPrototype>[] WallTags = ["Window"];

    public override void Initialize()
    {
        base.Initialize();

        // Data handling events
        SubscribeLocalEvent<NavMapComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ConfigurableNavMapBeaconComponent, ExaminedEvent>(OnConfigurableExamined);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetTileIndex(Vector2i relativeTile)
    {
        return relativeTile.X * ChunkSize + relativeTile.Y;
    }

    /// <summary>
    /// Inverse of <see cref="GetTileIndex"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetTileFromIndex(int index)
    {
        var x = index / ChunkSize;
        var y = index % ChunkSize;
        return new Vector2i(x, y);
    }

    public NavMapChunkType GetEntityType(EntityUid uid)
    {
        if (_doorQuery.HasComp(uid))
            return NavMapChunkType.Airlock;

        if (_wallQuery.HasComp(uid) || _tagSystem.HasAnyTag(uid, WallTags))
            return NavMapChunkType.Wall;

        return NavMapChunkType.Invalid;
    }

    protected bool TryCreateNavMapBeaconData(EntityUid uid, NavMapBeaconComponent component, TransformComponent xform, MetaDataComponent meta, [NotNullWhen(true)] out NavMapBeacon? beaconData)
    {
        beaconData = null;

        if (!component.Enabled || xform.GridUid == null || !xform.Anchored)
            return false;

        var name = component.Text;
        if (string.IsNullOrEmpty(name))
            name = meta.EntityName;

        beaconData = new NavMapBeacon(meta.NetEntity, component.Color, name, xform.LocalPosition);

        return true;
    }

    public void AddOrUpdateNavMapRegion(EntityUid uid, NavMapComponent component, NetEntity regionOwner, NavMapRegionProperties regionProperties)
    {
        // Check if a new region has been added or an existing one has been altered
        var isDirty = !component.RegionProperties.TryGetValue(regionOwner, out var oldProperties) || oldProperties != regionProperties;

        if (isDirty)
        {
            component.RegionProperties[regionOwner] = regionProperties;

            if (_net.IsServer)
                Dirty(uid, component);
        }
    }

    public void RemoveNavMapRegion(EntityUid uid, NavMapComponent component, NetEntity regionOwner)
    {
        bool regionOwnerRemoved = component.RegionProperties.Remove(regionOwner) | component.RegionOverlays.Remove(regionOwner);

        if (regionOwnerRemoved)
        {
            if (component.RegionOwnerToChunkTable.TryGetValue(regionOwner, out var affectedChunks))
            {
                foreach (var affectedChunk in affectedChunks)
                {
                    if (component.ChunkToRegionOwnerTable.TryGetValue(affectedChunk, out var regionOwners))
                        regionOwners.Remove(regionOwner);
                }

                component.RegionOwnerToChunkTable.Remove(regionOwner);
            }

            if (_net.IsServer)
                Dirty(uid, component);
        }
    }

    public Dictionary<NetEntity, NavMapRegionOverlay> GetNavMapRegionOverlays(EntityUid uid, NavMapComponent component, Enum uiKey)
    {
        var regionOverlays = new Dictionary<NetEntity, NavMapRegionOverlay>();

        foreach (var (regionOwner, regionOverlay) in component.RegionOverlays)
        {
            if (!regionOverlay.UiKey.Equals(uiKey))
                continue;

            regionOverlays.Add(regionOwner, regionOverlay);
        }

        return regionOverlays;
    }

    #region: Event handling

    private void OnGetState(EntityUid uid, NavMapComponent component, ref ComponentGetState args)
    {
        Dictionary<Vector2i, int[]> chunks;

        // Should this be a full component state or a delta-state?
        if (args.FromTick <= component.CreationTick)
        {
            // Full state
            chunks = new(component.Chunks.Count);
            foreach (var (origin, chunk) in component.Chunks)
            {
                chunks.Add(origin, chunk.TileData);
            }

            args.State = new NavMapState(chunks, component.Beacons, component.RegionProperties);
            return;
        }

        chunks = new();
        foreach (var (origin, chunk) in component.Chunks)
        {
            if (chunk.LastUpdate < args.FromTick)
                continue;

            chunks.Add(origin, chunk.TileData);
        }

        args.State = new NavMapDeltaState(chunks, component.Beacons, component.RegionProperties, new(component.Chunks.Keys));
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

    #region: System messages

    [Serializable, NetSerializable]
    protected sealed class NavMapState(
        Dictionary<Vector2i, int[]> chunks,
        Dictionary<NetEntity, NavMapBeacon> beacons,
        Dictionary<NetEntity, NavMapRegionProperties> regions)
        : ComponentState
    {
        public Dictionary<Vector2i, int[]> Chunks = chunks;
        public Dictionary<NetEntity, NavMapBeacon> Beacons = beacons;
        public Dictionary<NetEntity, NavMapRegionProperties> Regions = regions;
    }

    [Serializable, NetSerializable]
    protected sealed class NavMapDeltaState(
        Dictionary<Vector2i, int[]> modifiedChunks,
        Dictionary<NetEntity, NavMapBeacon> beacons,
        Dictionary<NetEntity, NavMapRegionProperties> regions,
        HashSet<Vector2i> allChunks)
        : ComponentState, IComponentDeltaState<NavMapState>
    {
        public Dictionary<Vector2i, int[]> ModifiedChunks = modifiedChunks;
        public Dictionary<NetEntity, NavMapBeacon> Beacons = beacons;
        public Dictionary<NetEntity, NavMapRegionProperties> Regions = regions;
        public HashSet<Vector2i> AllChunks = allChunks;

        public void ApplyToFullState(NavMapState state)
        {
            foreach (var key in state.Chunks.Keys)
            {
                if (!AllChunks!.Contains(key))
                    state.Chunks.Remove(key);
            }

            foreach (var (index, data) in ModifiedChunks)
            {
                if (!state.Chunks.TryGetValue(index, out var stateValue))
                    state.Chunks[index] = stateValue = new int[data.Length];

                Array.Copy(data, stateValue, data.Length);
            }

            state.Beacons.Clear();
            foreach (var (nuid, beacon) in Beacons)
            {
                state.Beacons.Add(nuid, beacon);
            }

            state.Regions.Clear();
            foreach (var (nuid, region) in Regions)
            {
                state.Regions.Add(nuid, region);
            }
        }

        public NavMapState CreateNewFullState(NavMapState state)
        {
            var chunks = new Dictionary<Vector2i, int[]>(state.Chunks.Count);

            foreach (var (index, data) in state.Chunks)
            {
                if (!AllChunks!.Contains(index))
                    continue;

                var newData = chunks[index] = new int[ArraySize];

                if (ModifiedChunks.TryGetValue(index, out var updatedData))
                    Array.Copy(newData, updatedData, ArraySize);
                else
                    Array.Copy(newData, data, ArraySize);
            }

            return new NavMapState(chunks, new(Beacons), new(Regions));
        }
    }

    [Serializable, NetSerializable]
    public record struct NavMapBeacon(NetEntity NetEnt, Color Color, string Text, Vector2 Position);

    [Serializable, NetSerializable]
    public record struct NavMapRegionProperties(NetEntity Owner, Enum UiKey, HashSet<Vector2i> Seeds)
    {
        // Server defined color for the region
        public Color Color = Color.White;

        // The maximum number of tiles that can be assigned to this region
        public int MaxArea = 625;

        // The maximum distance this region can propagate from its seeds
        public int MaxRadius = 25;
    }

    #endregion

    #region API
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
    /// Returns a string describing the rough distance and direction
    /// to the position of <paramref name="ent"/> from the nearest beacon.
    /// </summary>
    [PublicAPI]
    public string GetNearestBeaconString(Entity<TransformComponent?> ent, bool onlyName = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return Loc.GetString("nav-beacon-pos-no-beacons");

        return GetNearestBeaconString(_transform.GetMapCoordinates(ent, ent.Comp), onlyName);
    }

    /// <summary>
    /// Returns a string describing the rough distance and direction
    /// to <paramref name="coordinates"/> from the nearest beacon.
    /// </summary>

    public string GetNearestBeaconString(MapCoordinates coordinates, bool onlyName = false)
    {
        if (!TryGetNearestBeacon(coordinates, out var beacon, out var pos))
            return Loc.GetString("nav-beacon-pos-no-beacons");

        if (onlyName)
            return beacon.Value.Comp.Text!;

        var gridOffset = Angle.Zero;
        if (MapSystem.TryFindGridAt(pos.Value, out var grid, out _))
            gridOffset = Transform(grid).LocalRotation;

        // get the angle between the two positions, adjusted for the grid rotation so that
        // we properly preserve north in relation to the grid.
        var offset = coordinates.Position - pos.Value.Position;
        var dir = offset.ToWorldAngle();
        var adjustedDir = (dir - gridOffset).GetDir();

        var length = offset.Length();
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

        return TryGetNearestBeacon(_transform.GetMapCoordinates(ent, ent.Comp), out beacon, out beaconCoords);
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

            var coords = _transform.GetWorldPosition(xform);
            var distanceSquared = (coordinates.Position - coords).LengthSquared();
            if (!float.IsInfinity(minDistance) && distanceSquared >= minDistance)
                continue;

            minDistance = distanceSquared;
            beacon = (uid, navBeacon);
            beaconCoords = new MapCoordinates(coords, xform.MapID);
        }

        return beacon != null;
    }
    #endregion

    protected void UpdateBeaconEnabledVisuals(Entity<NavMapBeaconComponent> ent)
    {
        _appearance.SetData(ent, NavMapBeaconVisuals.Enabled, ent.Comp.Enabled && Transform(ent).Anchored);
    }
}
