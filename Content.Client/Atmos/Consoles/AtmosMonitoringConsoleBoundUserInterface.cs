using Content.Shared.Atmos.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.Consoles;

/// <summary>
/// A BUI for the atmospheric network monitor, wraps an <see cref="AtmosMonitoringConsoleWindow"/>
/// </summary>
/// <seealso cref="AtmosMonitoringConsoleComponent"/>
public sealed class AtmosMonitoringConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AtmosMonitoringConsoleWindow? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AtmosMonitoringConsoleWindow>();
        _menu.SetConsole(Owner);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AtmosMonitoringConsoleBoundInterfaceState castState)
            return;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
        _menu?.UpdateUI(xform?.Coordinates, castState.AtmosNetworks);
    }
}
