using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.Atmos.Monitor.UI
{
    public class AirAlarmBoundUserInterface : BoundUserInterface
    {
        private AirAlarmWindow? _window;

        public AirAlarmBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {}

        protected override void Open()
        {
            base.Open();

            _window = new AirAlarmWindow();

            if (State != null) UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.AtmosDeviceDataChanged += OnDeviceDataChanged;
            _window.AtmosAlarmThresholdChanged += OnThresholdChanged;
            _window.AirAlarmModeChanged += OnAirAlarmModeChanged;
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

        private void OnAirAlarmModeChanged(AirAlarmMode mode)
        {
            SendMessage(new AirAlarmUpdateAlarmModeMessage(mode));
        }

        private void OnThresholdChanged(AtmosMonitorThresholdType type, AtmosAlarmThreshold threshold, Gas? gas = null)
        {
            SendMessage(new AirAlarmUpdateAlarmThresholdMessage(type, threshold, gas));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null)
                return;

            switch (message)
            {
                case AirAlarmSetAddressMessage addrMsg:
                    _window.SetAddress(addrMsg.Address);
                    break;
                case AirAlarmUpdateDeviceDataMessage deviceMsg:
                    _window.UpdateDeviceData(deviceMsg.Address, deviceMsg.Data);
                    break;
                case AirAlarmUpdateAlarmModeMessage alarmMsg:
                    _window.UpdateModeSelector(alarmMsg.Mode);
                    break;
                case AirAlarmUpdateAlarmThresholdMessage thresholdMsg:
                    _window.UpdateThreshold(ref thresholdMsg);
                    break;
                case AirAlarmUpdateAirDataMessage airDataMsg:
                    _window.UpdateGasData(ref airDataMsg.AirData);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) _window?.Dispose();
        }
    }
}
