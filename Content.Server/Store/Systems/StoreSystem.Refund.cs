using Content.Server.Store.Components;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem
{
    private void InitializeRefund()
    {
        SubscribeLocalEvent<StoreRefundComponent, ComponentShutdown>(OnRefundShutdown);
    }

    private void OnRefundShutdown(EntityUid uid, StoreRefundComponent component, ComponentShutdown args)
    {
        // TODO: Check if an entity is being deleted with metadata

        var ev = new RefundEntityDeletedEvent(uid);
        RaiseLocalEvent(component.StoreEntity, ev);
    }
}
