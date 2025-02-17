using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;

namespace Content.Client.Backmen.Blob;

public sealed class BlobCarrierSystem : SharedBlobCarrierSystem
{
    protected override void TransformToBlob(Entity<BlobCarrierComponent> ent)
    {
        // do nothing
    }
}
