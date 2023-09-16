using Content.Client.Power.APC.UI;
using Content.Shared.APC;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC
{
    [UsedImplicitly]
    public sealed class ApcBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ApcMenu? _menu;

        [ViewVariables]
        private PowerMonitoringWindow? _monitoringWindow;

        public ApcBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new ApcMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();

            _monitoringWindow = new PowerMonitoringWindow();
            _monitoringWindow.OnClose += Close;
            _monitoringWindow.OpenCenteredRight();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ApcBoundInterfaceState) state;
            _menu?.UpdateState(castState);
        }

        public void BreakerPressed()
        {
            SendMessage(new ApcToggleMainBreakerMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _menu?.Dispose();
            }
        }
    }
}
