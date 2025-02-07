using Content.Client.Atmos.UI;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping.Binary.Components;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasPressurePumpSystem : SharedGasPressurePumpSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasPressurePumpComponent, AfterAutoHandleStateEvent>(OnPumpUpdate);
    }

    private void OnPumpUpdate(Entity<GasPressurePumpComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (UserInterfaceSystem.TryGetOpenUi<GasPressurePumpBoundUserInterface>(ent.Owner, GasPressurePumpUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
