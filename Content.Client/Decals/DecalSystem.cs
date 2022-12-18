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

        private DecalOverlay _overlay = default!;
        public readonly Dictionary<EntityUid, SortedDictionary<int, SortedDictionary<uint, Decal>>> DecalRenderIndex = new();
        private readonly Dictionary<EntityUid, Dictionary<uint, int>> _decalZIndexIndex = new();

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(this, _sprites, EntityManager, PrototypeManager);
            _overlayManager.AddOverlay(_overlay);

            SubscribeLocalEvent<DecalGridComponent, ComponentHandleState>(OnHandleState);
            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
        }

        public void ToggleOverlay()
        {
            if (_overlayManager.HasOverlay<DecalOverlay>())
            {
                _overlayManager.RemoveOverlay(_overlay);
            }
            else
            {
                _overlayManager.AddOverlay(_overlay);
            }
        }

        private void OnGridRemoval(GridRemovalEvent ev)
        {
            DecalRenderIndex.Remove(ev.EntityUid);
            _decalZIndexIndex.Remove(ev.EntityUid);
        }

        private void OnGridInitialize(GridInitializeEvent ev)
        {
            DecalRenderIndex[ev.EntityUid] = new();
            _decalZIndexIndex[ev.EntityUid] = new();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlayManager.RemoveOverlay(_overlay);
        }

        protected override bool RemoveDecalHook(EntityUid gridId, uint uid)
        {
            RemoveDecalFromRenderIndex(gridId, uid);
            return base.RemoveDecalHook(gridId, uid);
        }

        private void RemoveDecalFromRenderIndex(EntityUid gridId, uint uid)
        {
            var zIndex = _decalZIndexIndex[gridId][uid];

            DecalRenderIndex[gridId][zIndex].Remove(uid);
            if (DecalRenderIndex[gridId][zIndex].Count == 0)
                DecalRenderIndex[gridId].Remove(zIndex);

            _decalZIndexIndex[gridId].Remove(uid);
        }

        private void OnHandleState(EntityUid gridUid, DecalGridComponent gridComp, ref ComponentHandleState args)
        {
            if (args.Current is not DecalGridState state)
                return;

            // is this a delta or full state?
            var removedChunks = new List<Vector2i>();
            if (!state.FullState)
            {
                foreach (var key in gridComp.ChunkCollection.ChunkCollection.Keys)
                {
                    if (!state.AllChunks!.Contains(key))
                        removedChunks.Add(key);
                }
            }
            else
            {
                foreach (var key in gridComp.ChunkCollection.ChunkCollection.Keys)
                {
                    if (!state.Chunks.ContainsKey(key))
                        removedChunks.Add(key);
                }
            }

            if (removedChunks.Count > 0)
                RemoveChunks(gridUid, gridComp, removedChunks);

            if (state.Chunks.Count > 0)
                UpdateChunks(gridUid, gridComp, state.Chunks);
        }

        private void OnChunkUpdate(DecalChunkUpdateEvent ev)
        {
            foreach (var (gridId, updatedGridChunks) in ev.Data)
            {
                if (updatedGridChunks.Count == 0) continue;

                if (!TryComp(gridId, out DecalGridComponent? gridComp))
                {
                    Logger.Error($"Received decal information for an entity without a decal component: {ToPrettyString(gridId)}");
                    continue;
                }

                UpdateChunks(gridId, gridComp, updatedGridChunks);
            }

            // Now we'll cull old chunks out of range as the server will send them to us anyway.
            foreach (var (gridId, chunks) in ev.RemovedChunks)
            {
                if (chunks.Count == 0) continue;

                if (!TryComp(gridId, out DecalGridComponent? gridComp))
                {
                    Logger.Error($"Received decal information for an entity without a decal component: {ToPrettyString(gridId)}");
                    continue;
                }

                RemoveChunks(gridId, gridComp, chunks);
            }
        }

        private void UpdateChunks(EntityUid gridId, DecalGridComponent gridComp, Dictionary<Vector2i, DecalChunk> updatedGridChunks)
        {
            var chunkCollection = gridComp.ChunkCollection.ChunkCollection;

            if (!ChunkIndex.TryGetValue(gridId, out var chunkIndex) ||
                !DecalRenderIndex.TryGetValue(gridId, out var renderIndex) ||
                !_decalZIndexIndex.TryGetValue(gridId, out var zIndexIndex))
            {
                Logger.Error($"Grid missing from dictionaries while updating decal chunks for grid {ToPrettyString(gridId)}");
                return;
            }

            // Update any existing data / remove decals we didn't receive data for.
            foreach (var (indices, newChunkData) in updatedGridChunks)
            {
                if (chunkCollection.TryGetValue(indices, out var chunk))
                {
                    var removedUids = new HashSet<uint>(chunk.Decals.Keys);
                    removedUids.ExceptWith(newChunkData.Decals.Keys);
                    foreach (var removedUid in removedUids)
                    {
                        RemoveDecalHook(gridId, removedUid);
                        chunkIndex.Remove(removedUid);
                    }
                }

                chunkCollection[indices] = newChunkData;

                foreach (var (uid, decal) in newChunkData.Decals)
                {
                    if (zIndexIndex.TryGetValue(uid, out var zIndex))
                        renderIndex[zIndex].Remove(uid);

                    renderIndex.GetOrNew(decal.ZIndex)[uid] = decal;
                    zIndexIndex[uid] = decal.ZIndex;
                    chunkIndex[uid] = indices;
                }
            }
        }

        private void RemoveChunks(EntityUid gridId, DecalGridComponent gridComp, IEnumerable<Vector2i> chunks)
        {
            var chunkCollection = gridComp.ChunkCollection.ChunkCollection;

            if (!ChunkIndex.TryGetValue(gridId, out var chunkIndex))
            {
                Logger.Error($"Missing grid in ChunkIndex dictionary while removing chunks from grid {ToPrettyString(gridId)}");
                return;
            }

            foreach (var index in chunks)
            {
                if (!chunkCollection.TryGetValue(index, out var chunk)) continue;

                foreach (var uid  in chunk.Decals.Keys)
                {
                    RemoveDecalHook(gridId, uid);
                    chunkIndex.Remove(uid);
                }

                chunkCollection.Remove(index);
            }
        }
    }
}
