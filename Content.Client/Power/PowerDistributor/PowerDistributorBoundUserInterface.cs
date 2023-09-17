using Content.Shared.Power;
using JetBrains.Annotations;

namespace Content.Client.Power.PowerDistributor
{
    [UsedImplicitly]
    public sealed class PowerDistributorBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private PowerDistributorWindow? _window;

        public PowerDistributorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new PowerDistributorWindow(this);
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (PowerDistributorBoundInterfaceState) state;
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
