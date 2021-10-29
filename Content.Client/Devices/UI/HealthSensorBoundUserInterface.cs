using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Devices;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Devices.UI
{
    [UsedImplicitly]
    public class HealthSensorBoundUserInterface : BoundUserInterface
    {
        private HealthSensorWindow? _window;

        public HealthSensorBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _window = new HealthSensorWindow();
            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnResetPressed += ResetSensor;
            _window.OnSensorOptionSelectedEvent += SetSensorMode;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not HealthSensorBoundUserInterfaceState cast)
                return;

            _window.SetActive(cast.IsActive);
            _window.SetMode(cast.Mode);
        }

        private void ResetSensor()
        {
            SendMessage(new HealthSensorResetMessage());
        }

        private void SetSensorMode(int mode)
        {
            SendMessage(new HealthSensorUpdateModeMessage(mode));
        }
    }
}
