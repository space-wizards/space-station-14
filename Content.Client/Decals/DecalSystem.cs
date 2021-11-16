using System.Collections.Generic;
using Content.Shared.Decals;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Decals
{
    public class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        private DecalOverlay _overlay = default!;
        public Dictionary<GridId, SortedDictionary<int, SortedDictionary<uint, Decal>>> DecalRenderIndex = new();
        private Dictionary<GridId, Dictionary<uint, int>> DecalZIndexIndex = new();

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(this, MapManager, PrototypeManager);
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

        protected override bool RemoveDecalHook(GridId gridId, uint uid)
        {
            RemoveDecalFromRenderIndex(gridId, uid);

            return base.RemoveDecalHook(gridId, uid);
        }

        private void RemoveDecalFromRenderIndex(GridId gridId, uint uid)
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
                foreach (var (indices, newChunkData) in gridChunks)
                {
                    var chunkCollection = ChunkCollection(gridId);
                    if (chunkCollection.TryGetValue(indices, out var chunk))
                    {
                        var removedUids = new HashSet<uint>(chunk.Keys);
                        removedUids.ExceptWith(newChunkData.Keys);
                        foreach (var removedUid in removedUids)
                        {
                            RemoveDecalFromRenderIndex(gridId, removedUid);
                        }
                    }
                    foreach (var (uid, decal) in newChunkData)
                    {
                        if(!DecalRenderIndex[gridId].ContainsKey(decal.ZIndex))
                            DecalRenderIndex[gridId][decal.ZIndex] = new();

                        if (DecalZIndexIndex.TryGetValue(gridId, out var values) && values.TryGetValue(uid, out var zIndex))
                        {
                            DecalRenderIndex[gridId][zIndex].Remove(uid);
                        }

                        DecalRenderIndex[gridId][decal.ZIndex][uid] = decal;
                        DecalZIndexIndex[gridId][uid] = decal.ZIndex;

                        ChunkIndex[gridId][uid] = indices;
                    }
                    chunkCollection[indices] = newChunkData;
                }
            }
        }
    }
}
