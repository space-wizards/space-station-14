using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasVolumePumpWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class GasVolumePumpBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private const float MaxTransferRate = Atmospherics.MaxTransferRate;

        [ViewVariables]
        private GasVolumePumpWindow? _window;

        public GasVolumePumpBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<GasVolumePumpWindow>();

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.PumpTransferRateChanged += OnPumpTransferRatePressed;
        }

        private void OnToggleStatusButtonPressed()
        {
            if (_window is null) return;
            SendMessage(new GasVolumePumpToggleStatusMessage(_window.PumpStatus));
        }

        private void OnPumpTransferRatePressed(string value)
        {
            var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
            if (rate > MaxTransferRate)
                rate = MaxTransferRate;

            SendMessage(new GasVolumePumpChangeTransferRateMessage(rate));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not GasVolumePumpBoundUserInterfaceState cast)
                return;

            _window.Title = cast.PumpLabel;
            _window.SetPumpStatus(cast.Enabled);
            _window.SetTransferRate(cast.TransferRate);
        }
    }
}
