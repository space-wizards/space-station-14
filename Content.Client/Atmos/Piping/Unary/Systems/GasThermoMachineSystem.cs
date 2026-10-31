using Content.Client.Atmos.UI;
using Content.Shared.Atmos.Piping.Portable.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Piping.Unary.Systems;

namespace Content.Client.Atmos.Piping.Unary.Systems;

public sealed partial class GasThermoMachineSystem : SharedGasThermoMachineSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnGasAfterState(Entity<GasThermoMachineComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        DirtyUI(ent);

        if (_ui.TryGetOpenUi(ent.Owner, SpaceHeaterUiKey.Key, out var spaceHeaterUi))  // Space Heater also utilizes GasThermoMachineComponent
        {
            spaceHeaterUi.Update();
        }
    }

    protected override void DirtyUI(Entity<GasThermoMachineComponent> ent, UserInterfaceComponent? ui = null)
    {
        if (_ui.TryGetOpenUi<GasThermomachineBoundUserInterface>(ent.Owner, ThermomachineUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
