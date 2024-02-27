using Content.Shared.Blob.Components;

namespace Content.Shared.Blob;

public abstract partial class SharedBlobSystem
{
    public void InitializeResource()
    {
        SubscribeLocalEvent<BlobResourceComponent, BlobPulsedSetEvent>(OnResourcePulseSet);
        SubscribeLocalEvent<BlobResourceComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnResourcePulseSet(Entity<BlobResourceComponent> ent, ref BlobPulsedSetEvent args)
    {
        if (args.Pulsed)
            ent.Comp.NextResourceGen = _timing.CurTime + ent.Comp.Delay;
    }

    private void OnUnpaused(Entity<BlobResourceComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextResourceGen += args.PausedTime;
    }

    private void UpdateResource()
    {
        var query = EntityQueryEnumerator<BlobResourceComponent, BlobStructureComponent, BlobCreatedComponent>();
        while (query.MoveNext(out var uid, out var resource, out var blob, out var created))
        {
            if (_timing.CurTime < resource.NextResourceGen)
                continue;

            if (!blob.Pulsed)
                continue;

            if (created.Creator == null)
                continue;

            TryAddResource(created.Creator.Value, resource.Resource);
            resource.NextResourceGen += resource.Delay;
            resource.Delay += resource.DelayAccumulation;
            Dirty(uid, resource);
        }
    }
}
