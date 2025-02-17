using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Backmen.Blob.NPC.BlobPod;

namespace Content.Client.Backmen.Blob;

public sealed class BlobPodSystem : SharedBlobPodSystem
{
    public override bool NpcStartZombify(EntityUid uid, EntityUid argsTarget, BlobPodComponent component)
    {
        // do nothing
        return false;
    }
}
