using System;
using Content.Shared.Decals;
using Robust.Shared.GameObjects;

namespace Content.Client.Decals
{
    public class DecalSystem : SharedDecalSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
            SubscribeNetworkEvent<DecalRemovalUpdateEvent>(OnRemovalUpdate);
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
