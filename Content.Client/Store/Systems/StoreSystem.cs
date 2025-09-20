using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Systems;

namespace Content.Client.Store.Systems;

public sealed class StoreSystem : SharedStoreSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreComponent, AfterAutoHandleStateEvent>(OnStateUpdate);
    }

    private void OnStateUpdate(Entity<StoreComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        //UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<StoreComponent> ent)
    {
        if (Ui.TryGetOpenUi(ent.Owner, StoreUiKey.Key, out var bui))
            bui.Update();
    }
}
