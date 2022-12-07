using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.Atmos.Monitor.UI;

public sealed class AirAlarmBoundUserInterface : BoundUserInterface
{
    private AirAlarmWindow? _window;

    public AirAlarmBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new AirAlarmWindow(Owner);

        if (State != null)
        {
            UpdateState(State);
        }

        _window.OpenCentered();

        _window.OnClose += Close;
        _window.AtmosDeviceDataChanged += OnDeviceDataChanged;
        _window.AtmosAlarmThresholdChanged += OnThresholdChanged;
        _window.AirAlarmModeChanged += OnAirAlarmModeChanged;
        _window.ResyncAllRequested += ResyncAllDevices;
        _window.AirAlarmTabChange += OnTabChanged;
    }

    private void ResyncAllDevices()
    {
        SendMessage(new AirAlarmResyncAllDevicesMessage());
    }

    private void OnDeviceDataChanged(string address, IAtmosDeviceData data)
    {
        SendMessage(new AirAlarmUpdateDeviceDataMessage(address, data));
    }

    private void OnAirAlarmModeChanged(AirAlarmMode mode)
    {
        SendMessage(new AirAlarmUpdateAlarmModeMessage(mode));
    }

    private void OnThresholdChanged(string address, AtmosMonitorThresholdType type, AtmosAlarmThreshold threshold, Gas? gas = null)
    {
        SendMessage(new AirAlarmUpdateAlarmThresholdMessage(address, type, threshold, gas));
    }

    private void OnTabChanged(AirAlarmTab tab)
    {
        SendMessage(new AirAlarmTabSetMessage(tab));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AirAlarmUIState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing) _window?.Dispose();
    }
}
