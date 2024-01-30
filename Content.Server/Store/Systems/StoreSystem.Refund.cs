using Content.Server.Store.Components;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem
{
    private void InitializeRefund()
    {
        SubscribeLocalEvent<StoreRefundComponent, EntityTerminatingEvent>(OnRefundTerminating);
    }

    private void OnRefundTerminating(Entity<StoreRefundComponent> ent, ref EntityTerminatingEvent args)
    {
        var ev = new RefundEntityDeletedEvent(ent);
        RaiseLocalEvent(ent.Comp.StoreEntity, ref ev);
    }
}
