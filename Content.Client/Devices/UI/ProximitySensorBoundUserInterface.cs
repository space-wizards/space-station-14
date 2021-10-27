using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Devices;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Devices.UI
{
    [UsedImplicitly]
    public class ProximitySensorBoundUserInterface : BoundUserInterface
    {
        private ProximitySensorWindow? _window;

        public ProximitySensorBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _window = new ProximitySensorWindow();
            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnUpdateSensor += UpdateSensor;
            _window.OnActiveChanged += UpdateActive;
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
            if (_window == null || state is not ProximitySensorBoundUserInterfaceState cast)
                return;

            _window.SetRange(cast.Range);
            _window.SetActive(cast.IsActive);
            _window.SetArmingTime(cast.ArmingTime);
        }

        private void UpdateSensor(int range, int armingTime)
        {
            SendMessage(new ProximitySensorUpdateSensorMessage(range, armingTime));
        }

        private void UpdateActive(bool active)
        {
            SendMessage(new ProximitySensorUpdateActiveMessage(active));
        }
    }
}
