using Content.Client.Atmos.UI;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasTankSystem : SharedGasTankSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasTankComponent, AfterAutoHandleStateEvent>(OnGasTankState);
    }

    private void OnGasTankState(Entity<GasTankComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (UI.TryGetOpenUi<GasTankBoundUserInterface>(ent.Owner, SharedGasTankUiKey.Key, out var bui))
        {
            bui.Update(ent.Comp);
        }
    }

    public override void UpdateUserInterface(Entity<GasTankComponent> ent)
    {
        if (UI.TryGetOpenUi<GasTankBoundUserInterface>(ent.Owner, SharedGasTankUiKey.Key, out var bui))
        {
            bui.Update(ent.Comp);
        }
    }
}
