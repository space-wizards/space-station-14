using Content.Shared.Atmos.Piping.Binary.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasCanisterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class GasCanisterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private GasCanisterWindow? _window;

        public GasCanisterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<GasCanisterWindow>();

            _window.ReleaseValveCloseButtonPressed += OnReleaseValveClosePressed;
            _window.ReleaseValveOpenButtonPressed += OnReleaseValveOpenPressed;
            _window.ReleasePressureSet += OnReleasePressureSet;
            _window.TankEjectButtonPressed += OnTankEjectPressed;
        }

        private void OnTankEjectPressed()
        {
            SendMessage(new GasCanisterHoldingTankEjectMessage());
        }

        private void OnReleasePressureSet(float value)
        {
            SendMessage(new GasCanisterChangeReleasePressureMessage(value));
        }

        private void OnReleaseValveOpenPressed()
        {
            SendMessage(new GasCanisterChangeReleaseValveMessage(true));
        }

        private void OnReleaseValveClosePressed()
        {
            SendMessage(new GasCanisterChangeReleaseValveMessage(false));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not GasCanisterBoundUserInterfaceState cast)
                return;

            _window.SetCanisterLabel(cast.CanisterLabel);
            _window.SetCanisterPressure(cast.CanisterPressure);
            _window.SetPortStatus(cast.PortStatus);
            _window.SetTankLabel(cast.TankLabel);
            _window.SetTankPressure(cast.TankPressure);
            _window.SetReleasePressureRange(cast.ReleasePressureMin, cast.ReleasePressureMax);
            _window.SetReleasePressure(cast.ReleasePressure);
            _window.SetReleaseValve(cast.ReleaseValve);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
