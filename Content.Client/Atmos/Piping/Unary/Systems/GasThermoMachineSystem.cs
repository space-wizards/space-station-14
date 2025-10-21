using Content.Client.Atmos.UI;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Piping.Unary.Systems;

namespace Content.Client.Atmos.Piping.Unary.Systems;

public sealed class GasThermoMachineSystem : SharedGasThermoMachineSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasThermoMachineComponent, AfterAutoHandleStateEvent>(OnGasAfterState);
    }

    private void OnGasAfterState(Entity<GasThermoMachineComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        DirtyUI(ent.Owner, ent.Comp);
    }

    protected override void DirtyUI(EntityUid uid, GasThermoMachineComponent? thermoMachine, UserInterfaceComponent? ui = null)
    {
        if (_ui.TryGetOpenUi<GasThermomachineBoundUserInterface>(uid, ThermomachineUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
