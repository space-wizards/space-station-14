using Content.Shared.Atmos.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.Consoles;

public sealed class AtmosMonitoringConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AtmosMonitoringConsoleWindow? _menu;

    public AtmosMonitoringConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AtmosMonitoringConsoleWindow>();
        _menu.SetOwner(Owner);
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
