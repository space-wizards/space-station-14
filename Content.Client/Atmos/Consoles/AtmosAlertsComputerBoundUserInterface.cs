using Content.Shared.Atmos.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.Consoles;

/// <summary>
/// A BUI for the atmos alerts computer, wraps an <see cref="AtmosAlertsComputerWindow"/>.
/// </summary>
/// <seealso cref="AtmosAlertsComputerComponent"/>
public sealed class AtmosAlertsComputerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AtmosAlertsComputerWindow? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AtmosAlertsComputerWindow>();
        _menu.SetOwner(Owner);

        // Set atmos monitoring message action
        _menu.SendFocusChangeMessageAction += SendFocusChangeMessage;
        _menu.SendDeviceSilencedMessageAction += SendDeviceSilencedMessage;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (AtmosAlertsComputerBoundInterfaceState) state;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
        _menu?.UpdateUI(xform?.Coordinates, castState.AirAlarms, castState.FireAlarms, castState.FocusData);
    }

    private void SendFocusChangeMessage(NetEntity? netEntity)
    {
        SendMessage(new AtmosAlertsComputerFocusChangeMessage(netEntity));
    }

    private void SendDeviceSilencedMessage(NetEntity netEntity, bool silenceDevice)
    {
        SendMessage(new AtmosAlertsComputerDeviceSilencedMessage(netEntity, silenceDevice));
    }
}
