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
    public sealed class DecalGridComponent : Component
    {
        [DataField("chunkCollection", serverOnly: true)]
        public DecalGridChunkCollection ChunkCollection = new(new ());

        /// <summary>
        ///     Tick at which PVS was last toggled. Ensures that all players receive a full update when toggling PVS.
        /// </summary>
        public GameTick ForceTick { get; set; }

        [DataDefinition]
        [Serializable, NetSerializable]
        public sealed class DecalChunk
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
            public uint NextUid;
        }
    }

    [Serializable, NetSerializable]
    public sealed class DecalGridState : ComponentState, IComponentDeltaState
    {
        public Dictionary<Vector2i, DecalChunk> Chunks;
        public bool FullState => AllChunks == null;

        // required to infer deleted/missing chunks for delta states
        public HashSet<Vector2i>? AllChunks;

        public DecalGridState(Dictionary<Vector2i, DecalChunk> chunks)
        {
            Chunks = chunks;
        }

        public void ApplyToFullState(ComponentState fullState)
        {
            DebugTools.Assert(!FullState);
            var state = (DecalGridState) fullState;
            DebugTools.Assert(state.FullState);

            foreach (var key in state.Chunks.Keys)
            {
                if (!AllChunks!.Contains(key))
                    state.Chunks.Remove(key);
            }

            foreach (var (chunk, data) in Chunks)
            {
                state.Chunks[chunk] = new(data);
            }
        }

        public ComponentState CreateNewFullState(ComponentState fullState)
        {
            DebugTools.Assert(!FullState);
            var state = (DecalGridState) fullState;
            DebugTools.Assert(state.FullState);

            var chunks = new Dictionary<Vector2i, DecalChunk>(state.Chunks.Count);

            foreach (var (chunk, data) in Chunks)
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
