using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.Atmos.Monitor.UI;

public sealed class AirAlarmBoundUserInterface : BoundUserInterface
{
    private AirAlarmWindow? _window;

    public AirAlarmBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AirAlarmWindow>();
        _window.SetEntity(Owner);

        _window.AtmosDeviceDataChanged += OnDeviceDataChanged;
		_window.AtmosDeviceDataCopied += OnDeviceDataCopied;
        _window.AtmosAlarmThresholdChanged += OnThresholdChanged;
        _window.AirAlarmModeChanged += OnAirAlarmModeChanged;
        _window.AutoModeChanged += OnAutoModeChanged;
        _window.ResyncAllRequested += ResyncAllDevices;
    }

    private void ResyncAllDevices()
    {
        SendMessage(new AirAlarmResyncAllDevicesMessage());
    }

    private void OnDeviceDataChanged(string address, IAtmosDeviceData data)
    {
        SendMessage(new AirAlarmUpdateDeviceDataMessage(address, data));
    }

	private void OnDeviceDataCopied(IAtmosDeviceData data)
    {
        SendMessage(new AirAlarmCopyDeviceDataMessage(data));
    }

    private void OnAirAlarmModeChanged(AirAlarmMode mode)
    {
        SendMessage(new AirAlarmUpdateAlarmModeMessage(mode));
    }

    private void OnAutoModeChanged(bool enabled)
    {
        SendMessage(new AirAlarmUpdateAutoModeMessage(enabled));
    }

    private void OnThresholdChanged(string address, AtmosMonitorThresholdType type, AtmosAlarmThreshold threshold, Gas? gas = null)
    {
        SendMessage(new AirAlarmUpdateAlarmThresholdMessage(address, type, threshold, gas));
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
