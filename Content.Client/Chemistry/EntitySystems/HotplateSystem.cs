using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class HotplateSystem : SharedHotplateSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotplateComponent, AfterAutoHandleStateEvent>(OnHotplateUpdated);
    }

    private void OnHotplateUpdated(Entity<HotplateComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<HotplateComponent> ent)
    {
        if (UI.TryGetOpenUi(ent.Owner, HotplateUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
