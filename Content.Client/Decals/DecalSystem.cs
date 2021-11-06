using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Decals;

namespace Content.Client.Decals
{
    public class DecalSystem : SharedDecalSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
            SubscribeNetworkEvent<DecalRemovalUpdateEvent>(OnRemovalUpdate);
            SubscribeNetworkEvent<DecalIndexCheckEvent>(OnIndexCheck);
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
