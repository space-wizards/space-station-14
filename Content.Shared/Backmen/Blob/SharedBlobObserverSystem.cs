using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;

namespace Content.Shared.Backmen.Blob;

public abstract class SharedBlobObserverSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobObserverComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<BlobObserverComponent, GetUsedEntityEvent>(OnGetUsedEntityEvent);
    }

    private void OnGetUsedEntityEvent(Entity<BlobObserverComponent> ent, ref GetUsedEntityEvent args)
    {
        if(ent.Comp.VirtualItem.Valid)
            args.Used = ent.Comp.VirtualItem;
    }

    private void OnUpdateCanMove(EntityUid uid, BlobObserverComponent component, UpdateCanMoveEvent args)
    {
        if (component.CanMove)
            return;

        args.Cancel();
    }
}
