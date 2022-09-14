using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasThermomachineWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class GasThermomachineBoundUserInterface : BoundUserInterface
    {
        private GasThermomachineWindow? _window;

        private float _minTemp = 0.0f;
        private float _maxTemp = 0.0f;

        public GasThermomachineBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new GasThermomachineWindow();

            if(State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;

            _window.ToggleStatusButton.OnPressed += _ => OnToggleStatusButtonPressed();
            _window.TemperatureSpinbox.OnValueChanged += _ => OnTemperatureChanged(_window.TemperatureSpinbox.Value);
        }

        private void OnToggleStatusButtonPressed()
        {
            if (_window is null) return;

            _window.SetActive(!_window.Active);
            SendMessage(new GasThermomachineToggleMessage());
        }

        private void OnTemperatureChanged(float value)
        {
            var actual = Math.Clamp(value, _minTemp, _maxTemp);
            if (!MathHelper.CloseTo(actual, value, 0.09))
            {
                _window?.SetTemperature(actual);
                return;
            }

            SendMessage(new GasThermomachineChangeTemperatureMessage(actual));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not GasThermomachineBoundUserInterfaceState cast)
                return;

            _minTemp = cast.MinTemperature;
            _maxTemp = cast.MaxTemperature;

            _window.SetTemperature(cast.Temperature);
            _window.SetActive(cast.Enabled);
            _window.Title = cast.Mode switch
            {
                ThermoMachineMode.Freezer => Loc.GetString("comp-gas-thermomachine-ui-title-freezer"),
                ThermoMachineMode.Heater => Loc.GetString("comp-gas-thermomachine-ui-title-heater"),
                _ => string.Empty
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
