using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

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

            var chunkCollection = gridComp.ChunkCollection.ChunkCollection;
            var chunkIndex = ChunkIndex[gridUid];
            var renderIndex = DecalRenderIndex[gridUid];
            var zIndexIndex = _decalZIndexIndex[gridUid];

            var oldCollection = gridComp.ChunkCollection.ChunkCollection;
            var newCollection = state.ChunkCollection.ChunkCollection;

            // This only works under the assumption that clients never predict decal changes. Otherwise, this needs to clone this data, not store a reference to it.
            gridComp.ChunkCollection = state.ChunkCollection;

 
            // Update any existing data / remove decals we didn't receive data for.
            foreach (var (indices, oldChunkData) in oldCollection)
            {
                var removed = new HashSet<uint>(oldChunkData.Keys);
                if (newCollection.TryGetValue(indices, out var newChunkData))
                    removed.ExceptWith(newChunkData.Keys);

                foreach (var id in removed)
                {
                    RemoveDecalHook(gridUid, id);
                }
            }

            foreach (var (indices, newChunkData) in newCollection)
            {
                oldCollection.TryGetValue(indices, out var oldChunkData);
                
                foreach (var (id, decal) in newChunkData)
                {
                    if (oldChunkData?.ContainsKey(id) ?? false)
                        continue;

                    if (zIndexIndex.TryGetValue(id, out var zIndex))
                        renderIndex[zIndex].Remove(id);

                    renderIndex.GetOrNew(decal.ZIndex)[id] = decal;
                    zIndexIndex[id] = decal.ZIndex;
                    chunkIndex[id] = indices;
                }
            }
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

                var chunkCollection = gridComp.ChunkCollection.ChunkCollection;
                var chunkIndex = ChunkIndex[gridId];
                var renderIndex = DecalRenderIndex[gridId];
                var zIndexIndex = _decalZIndexIndex[gridId];

                // Update any existing data / remove decals we didn't receive data for.
                foreach (var (indices, newChunkData) in updatedGridChunks)
                {
                    if (chunkCollection.TryGetValue(indices, out var chunk))
                    {
                        var removedUids = new HashSet<uint>(chunk.Keys);
                        removedUids.ExceptWith(newChunkData.Keys);
                        foreach (var removedUid in removedUids)
                        {
                            RemoveDecalHook(gridId, removedUid);
                            chunkIndex.Remove(removedUid);
                        }
                    }

                    chunkCollection[indices] = newChunkData;

                    foreach (var (uid, decal) in newChunkData)
                    {
                        if (zIndexIndex.TryGetValue(uid, out var zIndex))
                            renderIndex[zIndex].Remove(uid);

                        renderIndex.GetOrNew(decal.ZIndex)[uid] = decal;
                        zIndexIndex[uid] = decal.ZIndex;
                        chunkIndex[uid] = indices;
                    }
                }
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

                var chunkCollection = gridComp.ChunkCollection.ChunkCollection;
                var chunkIndex = ChunkIndex[gridId];

                foreach (var index in chunks)
                {
                    if (!chunkCollection.TryGetValue(index, out var chunk)) continue;

                    foreach (var (uid, _) in chunk)
                    {
                        RemoveDecalHook(gridId, uid);
                        chunkIndex.Remove(uid);
                    }

                    chunkCollection.Remove(index);
                }
            }
        }
    }
}
