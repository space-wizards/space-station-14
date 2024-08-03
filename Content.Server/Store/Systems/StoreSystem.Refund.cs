using Content.Server.Store.Components;
using Content.Shared.Store.Components;
using Robust.Shared.Containers;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem
{
    private void InitializeRefund()
    {
        SubscribeLocalEvent<StoreComponent, EntityTerminatingEvent>(OnStoreTerminating);
        SubscribeLocalEvent<StoreRefundComponent, EntityTerminatingEvent>(OnRefundTerminating);
        SubscribeLocalEvent<StoreRefundComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<StoreRefundComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
    }

    private void OnEntityRemoved(EntityUid uid, StoreRefundComponent component, EntRemovedFromContainerMessage args)
    {
        if (component.StoreEntity == null || _actions.TryGetActionData(uid, out _) || !TryComp<StoreComponent>(component.StoreEntity.Value, out var storeComp))
            return;

        DisableRefund(component.StoreEntity.Value, storeComp);
    }

    private void OnEntityInserted(EntityUid uid, StoreRefundComponent component, EntInsertedIntoContainerMessage args)
    {
        if (component.StoreEntity == null || _actions.TryGetActionData(uid, out _) || !TryComp<StoreComponent>(component.StoreEntity.Value, out var storeComp))
            return;

        DisableRefund(component.StoreEntity.Value, storeComp);
    }

    private void OnStoreTerminating(Entity<StoreComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.BoughtEntities.Count <= 0)
            return;

        foreach (var boughtEnt in ent.Comp.BoughtEntities)
        {
            if (!TryComp<StoreRefundComponent>(boughtEnt, out var refundComp))
                continue;

            refundComp.StoreEntity = null;
        }
    }

    private void OnRefundTerminating(Entity<StoreRefundComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.StoreEntity == null)
            return;

        var ev = new RefundEntityDeletedEvent(ent);
        RaiseLocalEvent(ent.Comp.StoreEntity.Value, ref ev);
    }
}
