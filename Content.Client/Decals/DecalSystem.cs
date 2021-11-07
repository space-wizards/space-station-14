using System;
using System.Collections.Generic;
using System.Linq;
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
            SubscribeNetworkEvent<DecalIndexCheckEvent>(OnIndexCheck);
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

        private void OnIndexCheck(DecalIndexCheckEvent ev)
        {
            var existingUids = ChunkIndex.Keys.ToHashSet();
            var missing = new HashSet<uint>(ev.SeenIndices);
            missing.ExceptWith(existingUids);
            if (missing.Count > 0)
            {
                throw new Exception($"Missing decals: {string.Join(',', missing)}");
            }
        }

        private void OnRemovalUpdate(DecalRemovalUpdateEvent msg)
        {
            foreach (var uid in msg.RemovedDecals)
            {
                RemoveDecalInternal(uid);
            }
        }

        private void OnChunkUpdate(DecalChunkUpdateEvent ev)
        {
            foreach (var (gridId, gridChunks) in ev.Data)
            {
                foreach (var (indices, newChunkData) in gridChunks)
                {
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
