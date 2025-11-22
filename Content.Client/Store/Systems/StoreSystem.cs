using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Systems;

namespace Content.Client.Store.Systems;

public sealed class StoreSystem : SharedStoreSystem
{
    protected override void UpdateUi(Entity<StoreComponent> ent)
    {
        if (Ui.TryGetOpenUi(ent.Owner, StoreUiKey.Key, out var bui))
            bui.Update();
    }
}
