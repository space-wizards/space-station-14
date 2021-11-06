using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Decals;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Decals
{
    public class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        private DecalOverlay _overlay = default!;
        public Dictionary<GridId, ChunkCollection<Dictionary<uint, Decal>>> ChunkCollectionsForRendering => ChunkCollections;

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(this, MapManager, PrototypeManager);
            _overlayManager.AddOverlay(_overlay);

            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
            SubscribeNetworkEvent<DecalRemovalUpdateEvent>(OnRemovalUpdate);
            SubscribeNetworkEvent<DecalIndexCheckEvent>(OnIndexCheck);
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
            foreach (var (uid, (decal, gridId)) in ev.UpdatedDecals)
            {
                RegisterDecal(uid, decal, gridId);
            }
        }
    }
}
