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
        private Dictionary<uint, (GridId gridId, int zIndex)> DecalZIndexIndex = new();

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(this, MapManager, PrototypeManager);
            _overlayManager.AddOverlay(_overlay);

            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
            SubscribeNetworkEvent<DecalRemovalUpdateEvent>(OnRemovalUpdate);
            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
        }

        private void OnGridRemoval(GridRemovalEvent ev)
        {
            DecalRenderIndex.Remove(ev.GridId);
        }

        private void OnGridInitialize(GridInitializeEvent ev)
        {
            DecalRenderIndex[ev.GridId] = new();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlayManager.RemoveOverlay(_overlay);
        }

        private void OnRemovalUpdate(DecalRemovalUpdateEvent msg)
        {
            foreach (var uid in msg.RemovedDecals)
            {
                RemoveDecalInternal(uid);
            }
        }

        protected override bool RemoveDecalHook(uint uid)
        {
            RemoveDecalFromRenderIndex(uid);

            return base.RemoveDecalHook(uid);
        }

        private void RemoveDecalFromRenderIndex(uint uid)
        {
            var values = DecalZIndexIndex[uid];

            DecalRenderIndex[values.gridId][values.zIndex].Remove(uid);
            if (DecalRenderIndex[values.gridId][values.zIndex].Count == 0)
                DecalRenderIndex[values.gridId].Remove(values.zIndex);

            DecalZIndexIndex.Remove(uid);
        }

        private void OnChunkUpdate(DecalChunkUpdateEvent ev)
        {
            foreach (var (gridId, gridChunks) in ev.Data)
            {
                foreach (var (indices, newChunkData) in gridChunks)
                {
                    if (ChunkCollections[gridId].TryGetChunk(indices, out var chunk))
                    {
                        var removedUids = new HashSet<uint>(chunk.Keys);
                        removedUids.ExceptWith(newChunkData.Keys);
                        foreach (var removedUid in removedUids)
                        {
                            RemoveDecalFromRenderIndex(removedUid);
                        }
                    }
                    foreach (var (uid, decal) in newChunkData)
                    {
                        if (!DecalRenderIndex[gridId].TryGetValue(decal.ZIndex, out var decals))
                        {
                            DecalRenderIndex[gridId][decal.ZIndex] = new ();
                        }

                        if (DecalZIndexIndex.TryGetValue(uid, out var values))
                        {
                            DecalRenderIndex[values.gridId][values.zIndex].Remove(uid);
                        }

                        DecalRenderIndex[gridId][decal.ZIndex][uid] = decal;
                        DecalZIndexIndex[uid] = (gridId, decal.ZIndex);

                        ChunkIndex[uid] = (gridId, indices);
                    }
                    ChunkCollections[gridId].InsertChunk(indices, newChunkData);
                }
            }
        }
    }
}
