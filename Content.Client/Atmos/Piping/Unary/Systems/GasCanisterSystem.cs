using Content.Client.Atmos.UI;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Piping.Unary.Systems;
using Content.Shared.NodeContainer;

namespace Content.Client.Atmos.Piping.Unary.Systems;

public sealed class GasCanisterSystem : SharedGasCanisterSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasCanisterComponent, AfterAutoHandleStateEvent>(OnGasState);
    }

    private void OnGasState(Entity<GasCanisterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (UI.TryGetOpenUi<GasCanisterBoundUserInterface>(ent.Owner, GasCanisterUiKey.Key, out var bui))
        {
            bui.Update<GasCanisterBoundUserInterfaceState>();
        }
    }

    protected override void DirtyUI(EntityUid uid, GasCanisterComponent? component = null, NodeContainerComponent? nodes = null)
    {
        if (UI.TryGetOpenUi<GasCanisterBoundUserInterface>(uid, GasCanisterUiKey.Key, out var bui))
        {
            bui.Update<GasCanisterBoundUserInterfaceState>();
        }
    }
}
