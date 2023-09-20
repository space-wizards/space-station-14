using Content.Shared.Power;
using JetBrains.Annotations;

namespace Content.Client.Power.PowerMonitoring
{
    [UsedImplicitly]
    public sealed class PowerMonitoringDistributorBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private PowerMonitoringDistributorWindow? _window;

        public PowerMonitoringDistributorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new PowerMonitoringDistributorWindow(this);
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (PowerMonitoringBoundInterfaceState) state;
            _window?.UpdateState(castState);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
