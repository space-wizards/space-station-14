using Content.Server.Store.Components;
using Content.Shared.Actions.Events;
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
        SubscribeLocalEvent<StoreRefundComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnEntityRemoved(Entity<StoreRefundComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        CheckDisableRefund(ent);
    }

    private void OnEntityInserted(Entity<StoreRefundComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        CheckDisableRefund(ent);
    }

    private void OnActionPerformed(Entity<StoreRefundComponent> ent, ref ActionPerformedEvent args)
    {
        CheckDisableRefund(ent);
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

    private void CheckDisableRefund(Entity<StoreRefundComponent> ent)
    {
        var component = ent.Comp;

        // TODO: Should check for time & grid here
        if (component.StoreEntity == null || !TryComp<StoreComponent>(component.StoreEntity.Value, out var storeComp))
            return;

        DisableRefund(component.StoreEntity.Value, storeComp);
    }
}
