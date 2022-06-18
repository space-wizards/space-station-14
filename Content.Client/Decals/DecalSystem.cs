using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Decals
{
    public sealed class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly SharedTransformSystem _transforms = default!;
        [Dependency] private readonly SpriteSystem _sprites = default!;

        private DecalOverlay _overlay = default!;
        public Dictionary<EntityUid, SortedDictionary<int, SortedDictionary<uint, Decal>>> DecalRenderIndex = new();
        private Dictionary<EntityUid, Dictionary<uint, int>> DecalZIndexIndex = new();

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(this, _transforms, _sprites, EntityManager, MapManager, PrototypeManager);
            _overlayManager.AddOverlay(_overlay);

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
            DecalRenderIndex.Remove(ev.GridId);
            DecalZIndexIndex.Remove(ev.GridId);
        }

        private void OnGridInitialize(GridInitializeEvent ev)
        {
            DecalRenderIndex[ev.GridId] = new();
            DecalZIndexIndex[ev.GridId] = new();
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
            var zIndex = DecalZIndexIndex[gridId][uid];

            DecalRenderIndex[gridId][zIndex].Remove(uid);
            if (DecalRenderIndex[gridId][zIndex].Count == 0)
                DecalRenderIndex[gridId].Remove(zIndex);

            DecalZIndexIndex[gridId].Remove(uid);
        }

        private void OnChunkUpdate(DecalChunkUpdateEvent ev)
        {
            foreach (var (gridId, gridChunks) in ev.Data)
            {
                if (gridChunks.Count == 0) continue;

                var chunkCollection = ChunkCollection(gridId);

                // Update any existing data / remove decals we didn't receive data for.
                foreach (var (indices, newChunkData) in gridChunks)
                {
                    if (chunkCollection.TryGetValue(indices, out var chunk))
                    {
                        var removedUids = new HashSet<uint>(chunk.Keys);
                        removedUids.ExceptWith(newChunkData.Keys);
                        foreach (var removedUid in removedUids)
                        {
                            RemoveDecalInternal(gridId, removedUid);
                        }

                        chunkCollection[indices] = newChunkData;
                    }
                    else
                    {
                        chunkCollection.Add(indices, newChunkData);
                    }

                    foreach (var (uid, decal) in newChunkData)
                    {
                        if (!DecalRenderIndex[gridId].ContainsKey(decal.ZIndex))
                            DecalRenderIndex[gridId][decal.ZIndex] = new();

                        if (DecalZIndexIndex.TryGetValue(gridId, out var values) &&
                            values.TryGetValue(uid, out var zIndex))
                        {
                            DecalRenderIndex[gridId][zIndex].Remove(uid);
                        }

                        DecalRenderIndex[gridId][decal.ZIndex][uid] = decal;
                        DecalZIndexIndex[gridId][uid] = decal.ZIndex;
                        ChunkIndex[gridId][uid] = indices;
                    }
                }
            }

            // Now we'll cull old chunks out of range as the server will send them to us anyway.
            foreach (var (gridId, chunks) in ev.RemovedChunks)
            {
                if (chunks.Count == 0) continue;

                var chunkCollection = ChunkCollection(gridId);

                foreach (var index in chunks)
                {
                    if (!chunkCollection.TryGetValue(index, out var chunk)) continue;

                    foreach (var (uid, _) in chunk)
                    {
                        RemoveDecalInternal(gridId, uid);
                    }

                    chunkCollection.Remove(index);
                }
            }
        }
    }
}
