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

    private ThermobathComponent? thermobath;
    private ThermoregulatorComponent? thermoregulator;

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

        if (EntMan.TryGetComponent(Owner, out thermobath) && EntMan.TryGetComponent(Owner, out thermoregulator))
        {
            _window.SetMode(thermoregulator.Mode);
            _window.SetHysteresis(thermoregulator.Hysteresis);
            UpdateThermoBath(_window, thermobath);
            UpdateThermoRegulator(_window, thermoregulator);
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

        if (thermobath != null)
        {
            UpdateThermoBath(_window, thermobath);
        }

        if (thermoregulator != null)
        {
            UpdateThermoRegulator(_window, thermoregulator);
        }
    }

    private void UpdateThermoBath(ThermobathMenu window, ThermobathComponent comp)
    {
        window.SetBeakerPresent(comp.HasBeaker);
        window.SetSolutionTemperature(comp.SolutionTemperature);
    }

    private void UpdateThermoRegulator(ThermobathMenu window, ThermoregulatorComponent comp)
    {
        window.SetCurrentTemperature(comp.Temperature);
        window.SetTemperatureLimits(comp.MinTemperature, comp.MaxTemperature);
        window.SetSetpoint(comp.Setpoint, 5f); // We add tolerance to account for the temp exchange jitter
        window.UpdateStatusIndicators(comp.ActiveMode);
    }
}
