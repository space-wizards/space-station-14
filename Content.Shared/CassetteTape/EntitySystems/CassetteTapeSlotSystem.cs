using Content.Shared.CassetteTape.Components;
using Robust.Shared.Containers;

namespace Content.Shared.CassetteTape.EntitySystems;

public sealed class CassetteTapeSlotSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CassetteTapeSlotComponent, EntInsertedIntoContainerMessage>(OnTapeInserted);
        SubscribeLocalEvent<CassetteTapeSlotComponent, EntRemovedFromContainerMessage>(OnTapeRemoved);
        SubscribeLocalEvent<CassetteTapeSlotComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(EntityUid uid, CassetteTapeSlotComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.CassetteTapeSlotId)
            return;

        if (!HasComp<CassetteTapeBodyComponent>(args.EntityUid))
        {
            args.Cancel();
        }
    }

    private void OnTapeRemoved(EntityUid uid, CassetteTapeSlotComponent component, EntRemovedFromContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.CassetteTapeSlotId)
            return;

        // TODO: Change visual layer
        RaiseLocalEvent(uid, new CassetteTapeChangedEvent(true, args.Entity), false);
    }

    private void OnTapeInserted(EntityUid uid, CassetteTapeSlotComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.CassetteTapeSlotId)
            return;

        // TODO: Change visual layer
        RaiseLocalEvent(uid, new CassetteTapeChangedEvent(false, args.Entity), false);
    }
}
