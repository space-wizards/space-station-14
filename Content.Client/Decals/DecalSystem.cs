using Content.Client.Decals.Overlays;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Client.Decals
{
    public sealed class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly SpriteSystem _sprites = default!;

        private DecalOverlay? _overlay;
        private HashSet<Decal> _addedDecals = new();
        private HashSet<uint> _removedUids = new();
        private readonly List<Vector2i> _removedChunks = new();

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(_sprites, EntityManager, PrototypeManager);
            _overlayManager.AddOverlay(_overlay);

            SubscribeLocalEvent<DecalGridComponent, ComponentHandleState>(OnHandleState);
            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            if (_overlay == null)
                return;

            _overlayManager.RemoveOverlay(_overlay);
        }

        protected override void DirtyChunk(EntityUid id, Vector2i chunkIndices, DecalChunk chunk)
        {
            // NOOP for the client
        }

        public void ToggleOverlay()
        {
            if (_overlay == null)
                return;

            if (_overlayManager.HasOverlay<DecalOverlay>())
            {
                _overlayManager.RemoveOverlay(_overlay);
            }
            else
            {
                _overlayManager.AddOverlay(_overlay);
            }
        }

        private void OnHandleState(Entity<DecalGridComponent> ent, ref ComponentHandleState args)
        {
            // is this a delta or full state?
            _removedChunks.Clear();
            Dictionary<Vector2i, DecalChunk> modifiedChunks;

            switch (args.Current)
            {
                case DecalGridDeltaState delta:
                {
                    modifiedChunks = delta.ModifiedChunks;
                    foreach (var key in ent.Comp.ChunkCollection.ChunkCollection.Keys)
                    {
                        if (!delta.AllChunks.Contains(key))
                            _removedChunks.Add(key);
                    }

                    break;
                }
                case DecalGridState state:
                {
                    modifiedChunks = state.Chunks;
                    foreach (var key in ent.Comp.ChunkCollection.ChunkCollection.Keys)
                    {
                        if (!state.Chunks.ContainsKey(key))
                            _removedChunks.Add(key);
                    }

                    break;
                }
                default:
                    return;
            }

            if (_removedChunks.Count > 0)
                RemoveChunks(ent, _removedChunks);

            if (modifiedChunks.Count > 0)
                UpdateChunks(ent, modifiedChunks);
        }

        private void OnChunkUpdate(DecalChunkUpdateEvent ev)
        {
            foreach (var (netGrid, updatedGridChunks) in ev.Data)
            {
                if (updatedGridChunks.Count == 0)
                    continue;

                var gridId = GetEntity(netGrid);

                if (!TryComp(gridId, out DecalGridComponent? gridComp))
                {
                    Log.Error($"Received decal information for an entity without a decal component: {ToPrettyString(gridId)}");
                    continue;
                }

                UpdateChunks((gridId, gridComp), updatedGridChunks);
            }

            // Now we'll cull old chunks out of range as the server will send them to us anyway.
            foreach (var (netGrid, chunks) in ev.RemovedChunks)
            {
                if (chunks.Count == 0)
                    continue;

                var gridId = GetEntity(netGrid);

                if (!TryComp(gridId, out DecalGridComponent? gridComp))
                {
                    Log.Error($"Received decal information for an entity without a decal component: {ToPrettyString(gridId)}");
                    continue;
                }

                RemoveChunks((gridId, gridComp), chunks);
            }
        }

        private void UpdateChunks(Entity<DecalGridComponent> ent, Dictionary<Vector2i, DecalChunk> updatedGridChunks)
        {
            var chunkCollection = ent.Comp.ChunkCollection.ChunkCollection;

            // Update any existing data / remove decals we didn't receive data for.
            foreach (var (indices, newChunkData) in updatedGridChunks)
            {
                if (chunkCollection.TryGetValue(indices, out var chunk))
                {

                    _removedUids.Clear();
                    _removedUids.UnionWith(chunk.Decals.Keys);
                    _removedUids.ExceptWith(newChunkData.Decals.Keys);
                    foreach (var removedUid in _removedUids)
                    {
                        RemoveDecal((ent.Owner, ent.Comp), removedUid, out var _);
                        ent.Comp.DecalIndex.Remove(removedUid);
                    }

                    _addedDecals.Clear();
                    _addedDecals.UnionWith(newChunkData.Decals.Values);
                    _addedDecals.ExceptWith(chunk.Decals.Values);
                    foreach (var addedDecal in _addedDecals)
                    {
                        chunk.PredictedDecals.Remove(addedDecal);
                    }

                    newChunkData.PredictedDecals = chunk.PredictedDecals;
                    newChunkData.PredictedDecalDeletions = chunk.PredictedDecalDeletions;
                }

                chunkCollection[indices] = newChunkData;

                foreach (var (uid, decal) in newChunkData.Decals)
                {
                    ent.Comp.DecalIndex[uid] = indices;
                }
            }
        }

        private void RemoveChunks(Entity<DecalGridComponent> ent, IEnumerable<Vector2i> chunks)
        {
            var chunkCollection = ent.Comp.ChunkCollection.ChunkCollection;

            foreach (var index in chunks)
            {
                if (!chunkCollection.TryGetValue(index, out var chunk))
                    continue;

                foreach (var decalId  in chunk.Decals.Keys)
                {
                    RemoveDecal((ent.Owner, ent.Comp), decalId, out var _);
                    ent.Comp.DecalIndex.Remove(decalId);
                }

                chunkCollection.Remove(index);
            }
        }
    }
}
