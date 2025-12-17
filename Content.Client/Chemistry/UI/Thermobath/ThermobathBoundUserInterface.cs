using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chemistry.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI.Thermobath;

[UsedImplicitly]
public sealed class ThermobathBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    private readonly SharedPowerReceiverSystem _power;

    [ViewVariables]
    private ThermobathMenu? _window;

    private InputCoalescer<ThermoregulatorMode> _modeCoalescer;

    public ThermobathBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _power = EntMan.System<SharedPowerReceiverSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ThermobathMenu>();
        _window.SetInfoFromEntity(EntMan, Owner);

        _window.OnPowerChanged += powered => SendPredictedMessage(new ThermobathPowerChangedMessage(powered));
        _window.OnSetpointChanged += setpoint => SendPredictedMessage(new ThermobathSetpointChangedMessage(setpoint));
        // Sliders can't use predicted messsages and I don't know how to solve this with compstates
        _window.OnModeChanged += mode => _modeCoalescer.Set(mode);

        _window.SetPowered(_power.IsPowered(Owner));

        if (EntMan.TryGetComponent(Owner, out ThermobathComponent? thermbath) && EntMan.TryGetComponent(Owner, out ThermoregulatorComponent? thermoregulator))
        {
            _window.SetTemperatureLimits(thermoregulator.MinTemperature, thermoregulator.MaxTemperature);
            _window.SetCurrentTemperature(thermoregulator.Temperature);
            _window.SetSetpoint(thermoregulator.Setpoint);
            _window.SetMode(thermoregulator.Mode);
            _window.SetHysteresis(thermoregulator.Hysteresis);
            _window.UpdateStatusIndicators(thermoregulator.ActiveMode);

            _window.SetBeakerPresent(thermbath.HasBeaker);
            _window.SetSolutionTemperature(thermbath.SolutionTemperature);
        }
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        if (_modeCoalescer.CheckIsModified(out var modeValue))
            SendMessage(new ThermobathModeChangedMessage(modeValue));
    }

    public override void Update()
    {
        if (_window == null)
            return;

        _window.SetPowered(_power.IsPowered(Owner));

        if (EntMan.TryGetComponent(Owner, out ThermobathComponent? thermobath))
        {
            _window.SetBeakerPresent(thermobath.HasBeaker);
            _window.SetSolutionTemperature(thermobath.SolutionTemperature);
        }

        if (EntMan.TryGetComponent(Owner, out ThermoregulatorComponent? thermoregulator))
        {
            _window.SetCurrentTemperature(thermoregulator.Temperature);
            _window.SetTemperatureLimits(thermoregulator.MinTemperature, thermoregulator.MaxTemperature);
            _window.SetSetpoint(thermoregulator.Setpoint, 5f); // We add tolerance to account for the temp exchange jitter
            _window.UpdateStatusIndicators(thermoregulator.ActiveMode);
            // _window.SetMode(thermoregulator.Mode);
            // TODO: Sliders are jank so this only gets when we open the UI to hide the jittering,
            // but this should never be set by the server so we're fine.
        }
    }
}
