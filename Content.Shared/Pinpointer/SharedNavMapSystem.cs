using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Pinpointer;

public abstract class SharedNavMapSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public const byte ChunkSize = 4;

    public readonly NavMapChunkType[] EntityChunkTypes =
    {
        NavMapChunkType.Invalid,
        NavMapChunkType.Wall,
        NavMapChunkType.Airlock,
    };

    private readonly string[] _wallTags = ["Wall", "Window"];

    public override void Initialize()
    {
        base.Initialize();

        // Data handling events
        SubscribeLocalEvent<NavMapComponent, ComponentGetState>(OnGetState);
    }

    /// <summary>
    /// Converts the chunk's tile into a bitflag for the slot.
    /// </summary>
    public static int GetFlag(Vector2i relativeTile)
    {
        return 1 << (relativeTile.X * ChunkSize + relativeTile.Y);
    }

    /// <summary>
    /// Converts the chunk's tile into a bitflag for the slot.
    /// </summary>
    public static Vector2i GetTile(int flag)
    {
        var value = Math.Log2(flag);
        var x = (int) value / ChunkSize;
        var y = (int) value % ChunkSize;
        var result = new Vector2i(x, y);

        DebugTools.Assert(GetFlag(result) == flag);

        return new Vector2i(x, y);
    }

    public NavMapChunk SetAllEdgesForChunkTile(NavMapChunk chunk, Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);
        var flag = (ushort) GetFlag(relative);

        foreach (var (direction, _) in chunk.TileData)
            chunk.TileData[direction] |= flag;

        return chunk;
    }

    public NavMapChunk UnsetAllEdgesForChunkTile(NavMapChunk chunk, Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);
        var flag = (ushort) GetFlag(relative);
        var invFlag = (ushort) ~flag;

        foreach (var (direction, _) in chunk.TileData)
            chunk.TileData[direction] &= invFlag;

        return chunk;
    }

    public ushort GetCombinedEdgesForChunk(Dictionary<AtmosDirection, ushort> tile)
    {
        ushort combined = 0;

        foreach (var kvp in tile)
            combined |= kvp.Value;

        return combined;
    }

    public bool AllTileEdgesAreOccupied(Dictionary<AtmosDirection, ushort> tileData, Vector2i tile)
    {
        var flag = (ushort) GetFlag(tile);

        foreach (var kvp in tileData)
        {
            if ((kvp.Value & flag) == 0)
                return false;
        }

        return true;
    }

    public NavMapChunkType GetAssociatedEntityChunkType(EntityUid uid)
    {
        var category = NavMapChunkType.Invalid;

        if (HasComp<NavMapDoorComponent>(uid))
            category = NavMapChunkType.Airlock;

        else if (_tagSystem.HasAnyTag(uid, _wallTags))
            category = NavMapChunkType.Wall;

        return category;
    }

    protected bool TryCreateNavMapBeaconData(EntityUid uid, NavMapBeaconComponent component, TransformComponent xform, [NotNullWhen(true)] out NavMapBeacon? beaconData)
    {
        beaconData = null;

        if (!component.Enabled || xform.GridUid == null || !xform.Anchored)
            return false;

        string? name = component.Text;
        var meta = MetaData(uid);

        if (string.IsNullOrEmpty(name))
            name = meta.EntityName;

        beaconData = new NavMapBeacon(meta.NetEntity, component.Color, name, xform.LocalPosition)
        {
            LastUpdate = _gameTiming.CurTick
        };

        return true;
    }

    #region: Event handling

    private void OnGetState(EntityUid uid, NavMapComponent component, ref ComponentGetState args)
    {
        var chunks = new Dictionary<(NavMapChunkType, Vector2i), Dictionary<AtmosDirection, ushort>>();
        var beacons = new HashSet<NavMapBeacon>();

        // Should this be a full component state or a delta-state?
        if (args.FromTick <= component.CreationTick)
        {
            foreach (var ((category, origin), chunk) in component.Chunks)
            {
                var chunkDatum = new Dictionary<AtmosDirection, ushort>(chunk.TileData.Count);

                foreach (var (direction, tileData) in chunk.TileData)
                    chunkDatum[direction] = tileData;

                chunks.Add((category, origin), chunkDatum);
            }

            var beaconQuery = AllEntityQuery<NavMapBeaconComponent, TransformComponent>();

            while (beaconQuery.MoveNext(out var beaconUid, out var beacon, out var xform))
            {
                if (xform.GridUid != uid)
                    continue;

                if (!TryCreateNavMapBeaconData(beaconUid, beacon, xform, out var beaconData))
                    continue;

                beacons.Add(beaconData.Value);
            }

            args.State = new NavMapComponentState(chunks, beacons);
            return;
        }

        foreach (var ((category, origin), chunk) in component.Chunks)
        {
            if (chunk.LastUpdate < args.FromTick)
                continue;

            var chunkDatum = new Dictionary<AtmosDirection, ushort>(chunk.TileData.Count);

            foreach (var (direction, tileData) in chunk.TileData)
                chunkDatum[direction] = tileData;

            chunks.Add((category, origin), chunkDatum);
        }

        foreach (var beacon in component.Beacons)
        {
            if (beacon.LastUpdate < args.FromTick)
                continue;

            beacons.Add(beacon);
        }

        args.State = new NavMapComponentState(chunks, beacons)
        {
            AllChunks = new(component.Chunks.Keys),
            AllBeacons = new(component.Beacons)
        };
    }

    #endregion

    #region: System messages

    [Serializable, NetSerializable]
    protected sealed class NavMapComponentState : ComponentState, IComponentDeltaState
    {
        public Dictionary<(NavMapChunkType, Vector2i), Dictionary<AtmosDirection, ushort>> Chunks = new();
        public HashSet<NavMapBeacon> Beacons = new();

        // Required to infer deleted/missing chunks for delta states
        public HashSet<(NavMapChunkType, Vector2i)>? AllChunks;
        public HashSet<NavMapBeacon>? AllBeacons;

        public NavMapComponentState(Dictionary<(NavMapChunkType, Vector2i), Dictionary<AtmosDirection, ushort>> chunks, HashSet<NavMapBeacon> beacons)
        {
            Chunks = chunks;
            Beacons = beacons;
        }

        public bool FullState => (AllChunks == null || AllBeacons == null);

        public void ApplyToFullState(IComponentState fullState)
        {
            DebugTools.Assert(!FullState);
            var state = (NavMapComponentState) fullState;
            DebugTools.Assert(state.FullState);

            // Update chunks
            foreach (var key in state.Chunks.Keys)
            {
                if (!AllChunks!.Contains(key))
                    state.Chunks.Remove(key);
            }

            foreach (var (chunk, data) in Chunks)
                state.Chunks[chunk] = new(data);

            // Update beacons
            foreach (var beacon in state.Beacons)
            {
                if (!AllBeacons!.Contains(beacon))
                    state.Beacons.Remove(beacon);
            }

            foreach (var beacon in Beacons)
                state.Beacons.Add(beacon);
        }

        public IComponentState CreateNewFullState(IComponentState fullState)
        {
            DebugTools.Assert(!FullState);
            var state = (NavMapComponentState) fullState;
            DebugTools.Assert(state.FullState);

            var chunks = new Dictionary<(NavMapChunkType, Vector2i), Dictionary<AtmosDirection, ushort>>();
            var beacons = new HashSet<NavMapBeacon>();

            foreach (var (chunk, data) in Chunks)
                chunks[chunk] = new(data);

            foreach (var (chunk, data) in state.Chunks)
            {
                if (AllChunks!.Contains(chunk))
                    chunks.TryAdd(chunk, new(data));
            }

            foreach (var beacon in Beacons)
                beacons.Add(new NavMapBeacon(beacon.NetEnt, beacon.Color, beacon.Text, beacon.Position));

            foreach (var beacon in state.Beacons)
            {
                if (AllBeacons!.Contains(beacon))
                    beacons.Add(new NavMapBeacon(beacon.NetEnt, beacon.Color, beacon.Text, beacon.Position));
            }

            return new NavMapComponentState(chunks, beacons);
        }
    }

    [Serializable, NetSerializable]
    public record struct NavMapBeacon(NetEntity NetEnt, Color Color, string Text, Vector2 Position)
    {
        public GameTick LastUpdate;
    }

    #endregion
}
