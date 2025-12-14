using Content.Client.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Piping.Unary.Systems;
using Content.Shared.Power.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasThermomachineWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class GasThermomachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private GasThermomachineWindow? _window;

        [ViewVariables]
        private float _minTemp = 0.0f;

        [ViewVariables]
        private float _maxTemp = 0.0f;

        [ViewVariables]
        private bool _isHeater = true;

        public GasThermomachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<GasThermomachineWindow>();

            _window.ToggleStatusButton.OnPressed += _ => OnToggleStatusButtonPressed();
            _window.TemperatureSpinbox.OnValueChanged += _ => OnTemperatureChanged(_window.TemperatureSpinbox.Value);
            _window.Entity = Owner;
            Update();
        }

        private void OnToggleStatusButtonPressed()
        {
            if (_window is null) return;

            _window.SetActive(!_window.Active);
            SendPredictedMessage(new GasThermomachineToggleMessage());
        }

        private void OnTemperatureChanged(float value)
        {
            var actual = 0f;
            if (_isHeater)
                actual = Math.Min(value, _maxTemp);
            else
                actual = Math.Max(value, _minTemp);
            actual = Math.Max(actual, Atmospherics.TCMB);
            if (!MathHelper.CloseTo(actual, value, 0.09))
            {
                _window?.SetTemperature(actual);
                return;
            }

            SendPredictedMessage(new GasThermomachineChangeTemperatureMessage(actual));
        }

        public override void Update()
        {
            if (_window == null || !EntMan.TryGetComponent(Owner, out GasThermoMachineComponent? thermo))
                return;

            var system = EntMan.System<SharedGasThermoMachineSystem>();
            _minTemp = thermo.MinTemperature;
            _maxTemp = thermo.MaxTemperature;
            _isHeater = system.IsHeater(thermo);

            _window.SetTemperature(thermo.TargetTemperature);

            var receiverSys = EntMan.System<PowerReceiverSystem>();
            SharedApcPowerReceiverComponent? receiver = null;

            receiverSys.ResolveApc(Owner, ref receiver);

            // Also set in frameupdates.
            if (receiver != null)
            {
                _window.SetActive(!receiver.PowerDisabled);
            }

            _window.Title = _isHeater switch
            {
                false => Loc.GetString("comp-gas-thermomachine-ui-title-freezer"),
                true => Loc.GetString("comp-gas-thermomachine-ui-title-heater")
            };
        }
    }
}
