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
        if (MetaData(uid).EntityLifeStage == EntityLifeStage.Terminating)
        {
            var ev = new RefundEntityDeletedEvent(uid);
            RaiseLocalEvent(component.StoreEntity, ev);
        }
    }
}
