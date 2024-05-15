using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Pinpointer;

public abstract class SharedNavMapSystem : EntitySystem
{
    public const int Categories = 3;
    public const int Directions = 4; // Not directly tied to number of atmos directions

    public const int ChunkSize = 8;
    public const int ArraySize = ChunkSize* ChunkSize;

    public const int AllDirMask = (1 << Directions) - 1;
    public const int AirlockMask = AllDirMask << (int) NavMapChunkType.Airlock;
    public const int WallMask = AllDirMask << (int) NavMapChunkType.Wall;
    public const int FloorMask = AllDirMask << (int) NavMapChunkType.Floor;

    [Robust.Shared.IoC.Dependency] private readonly TagSystem _tagSystem = default!;

    private readonly string[] _wallTags = ["Wall", "Window"];
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
            return  NavMapChunkType.Airlock;

        if (_tagSystem.HasAnyTag(uid, _wallTags))
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

            args.State = new NavMapComponentState(chunks, component.Beacons);
            return;
        }

        chunks = new();
        foreach (var (origin, chunk) in component.Chunks)
        {
            if (chunk.LastUpdate < args.FromTick)
                continue;

            chunks.Add(origin, chunk.TileData);
        }

        args.State = new NavMapComponentState(chunks, component.Beacons)
        {
            // TODO NAVMAP cache a single AllChunks hashset in the component.
            // Or maybe just only send them if a chunk gets removed.
            AllChunks = new(component.Chunks.Keys),
        };
    }

    #endregion

    #region: System messages

    [Serializable, NetSerializable]
    protected sealed class NavMapComponentState(
        Dictionary<Vector2i, int[]> chunks,
        Dictionary<NetEntity, NavMapBeacon> beacons)
        : ComponentState, IComponentDeltaState
    {
        public Dictionary<Vector2i, int[]> Chunks = chunks;
        public Dictionary<NetEntity, NavMapBeacon> Beacons = beacons;

        // Required to infer deleted/missing chunks for delta states
        public HashSet<Vector2i>? AllChunks;

        public bool FullState => AllChunks == null;

        public void ApplyToFullState(IComponentState fullState)
        {
            DebugTools.Assert(!FullState);
            var state = (NavMapComponentState) fullState;
            DebugTools.Assert(state.FullState);

            foreach (var key in state.Chunks.Keys)
            {
                if (!AllChunks!.Contains(key))
                    state.Chunks.Remove(key);
            }

            foreach (var (index, data) in Chunks)
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
        }

        public IComponentState CreateNewFullState(IComponentState fullState)
        {
            DebugTools.Assert(!FullState);
            var state = (NavMapComponentState) fullState;
            DebugTools.Assert(state.FullState);

            var chunks = new Dictionary<Vector2i, int[]>(state.Chunks.Count);
            foreach (var (index, data) in state.Chunks)
            {
                if (!AllChunks!.Contains(index))
                    continue;

                var newData = chunks[index] = new int[ArraySize];

                if (Chunks.TryGetValue(index, out var updatedData))
                    Array.Copy(newData, updatedData, ArraySize);
                else
                    Array.Copy(newData, data, ArraySize);
            }

            return new NavMapComponentState(chunks, new(Beacons));
        }
    }

    [Serializable, NetSerializable]
    public record struct NavMapBeacon(NetEntity NetEnt, Color Color, string Text, Vector2 Position);

    #endregion
}
