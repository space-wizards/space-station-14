using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals
{
    [RegisterComponent]
    [Access(typeof(SharedDecalSystem))]
    [NetworkedComponent]
    public sealed partial class DecalGridComponent : Component
    {
        [Access(Other = AccessPermissions.ReadExecute)]
        [DataField(serverOnly: true)]
        public DecalGridChunkCollection ChunkCollection = new(new ());

        /// <summary>
        ///     Dictionary mapping decals to their corresponding grid chunks.
        /// </summary>
        public readonly Dictionary<uint, Vector2i> DecalIndex = new();

        /// <summary>
        ///     Tick at which PVS was last toggled. Ensures that all players receive a full update when toggling PVS.
        /// </summary>
        public GameTick ForceTick { get; set; }

        [DataDefinition]
        [Serializable, NetSerializable]
        public sealed partial class DecalChunk
        {
            [IncludeDataField(customTypeSerializer:typeof(DictionarySerializer<uint, Decal>))]
            public Dictionary<uint, Decal> Decals;

            [NonSerialized]
            public GameTick LastModified;

            public DecalChunk()
            {
                Decals = new();
            }

            public DecalChunk(Dictionary<uint, Decal> decals)
            {
                Decals = decals;
            }

            public DecalChunk(DecalChunk chunk)
            {
                // decals are readonly, so this should be fine.
                Decals = chunk.Decals.ShallowClone();
                LastModified = chunk.LastModified;
            }
        }

        [DataRecord, Serializable, NetSerializable]
        public record DecalGridChunkCollection(Dictionary<Vector2i, DecalChunk> ChunkCollection)
        {
            public uint NextDecalId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class DecalGridState(Dictionary<Vector2i, DecalChunk> chunks) : ComponentState
    {
        public Dictionary<Vector2i, DecalChunk> Chunks = chunks;
    }

    [Serializable, NetSerializable]
    public sealed class DecalGridDeltaState(Dictionary<Vector2i, DecalChunk> modifiedChunks, HashSet<Vector2i> allChunks)
        : ComponentState, IComponentDeltaState<DecalGridState>
    {
        public Dictionary<Vector2i, DecalChunk> ModifiedChunks = modifiedChunks;
        public HashSet<Vector2i> AllChunks = allChunks;

        public void ApplyToFullState(DecalGridState state)
        {
            foreach (var key in state.Chunks.Keys)
            {
                if (!AllChunks!.Contains(key))
                    state.Chunks.Remove(key);
            }

            foreach (var (chunk, data) in ModifiedChunks)
            {
                state.Chunks[chunk] = new(data);
            }
        }

        public DecalGridState CreateNewFullState(DecalGridState state)
        {
            var chunks = new Dictionary<Vector2i, DecalChunk>(state.Chunks.Count);

            foreach (var (chunk, data) in ModifiedChunks)
            {
                chunks[chunk] = new(data);
            }

            foreach (var (chunk, data) in state.Chunks)
            {
                if (AllChunks!.Contains(chunk))
                    chunks.TryAdd(chunk, new(data));
            }
            return new DecalGridState(chunks);
        }
    }
}
