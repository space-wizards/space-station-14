using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.IdentityManagement;
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
        private float _maxTransferRate;

        [ViewVariables]
        private GasVolumePumpWindow? _window;

        public GasVolumePumpBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<GasVolumePumpWindow>();

            if (EntMan.TryGetComponent(Owner, out GasVolumePumpComponent? pump))
            {
                _maxTransferRate = pump.MaxTransferRate;
            }

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.PumpTransferRateChanged += OnPumpTransferRatePressed;
            Update();
        }

        private void OnToggleStatusButtonPressed()
        {
            if (_window is null) return;

            SendPredictedMessage(new GasVolumePumpToggleStatusMessage(_window.PumpStatus));
        }

        private void OnPumpTransferRatePressed(string value)
        {
            var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
            rate = Math.Clamp(rate, 0f, _maxTransferRate);

            SendPredictedMessage(new GasVolumePumpChangeTransferRateMessage(rate));
        }

        public override void Update()
        {
            base.Update();

            if (_window is null || !EntMan.TryGetComponent(Owner, out GasVolumePumpComponent? pump))
                return;

            _window.Title = Identity.Name(Owner, EntMan);
            _window.SetPumpStatus(pump.Enabled);
            _window.SetTransferRate(pump.TransferRate);
        }
    }
}
