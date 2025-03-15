using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

public abstract class SharedNavMapSystem : EntitySystem
{
    public const int Categories = 3;
    public const int Directions = 4; // Not directly tied to number of atmos directions

    public const int ChunkSize = 8;
    public const int ArraySize = ChunkSize * ChunkSize;

    public const int AllDirMask = (1 << Directions) - 1;
    public const int AirlockMask = AllDirMask << (int) NavMapChunkType.Airlock;
    public const int WallMask = AllDirMask << (int) NavMapChunkType.Wall;
    public const int FloorMask = AllDirMask << (int) NavMapChunkType.Floor;

    [Robust.Shared.IoC.Dependency] private readonly TagSystem _tagSystem = default!;
    [Robust.Shared.IoC.Dependency] private readonly INetManager _net = default!;

    private static readonly ProtoId<TagPrototype>[] WallTags = {"Wall", "Window"};
    private EntityQuery<NavMapDoorComponent> _doorQuery;

    public override void Initialize()
    {
        base.Initialize();

        // Data handling events
        SubscribeLocalEvent<NavMapComponent, ComponentGetState>(OnGetState);
        _doorQuery = GetEntityQuery<NavMapDoorComponent>();
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

        if (_tagSystem.HasAnyTag(uid, WallTags))
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
}
