using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI.Thermobath;

[UsedImplicitly]
public sealed class ThermobathBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    private readonly SharedPowerReceiverSystem _power;
    private readonly SharedSolutionContainerSystem _solutions;
    private readonly ItemSlotsSystem _itemSlots;

    [ViewVariables]
    private ThermobathMenu? _window;

    private ThermoregulatorComponent? _thermoregulator;

    public ThermobathBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _power = EntMan.System<SharedPowerReceiverSystem>();
        _solutions = EntMan.System<SharedSolutionContainerSystem>();
        _itemSlots = EntMan.System<ItemSlotsSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ThermobathMenu>();
        _window.SetInfoFromEntity(EntMan, Owner);

        _window.OnPowerToggled += () => SendPredictedMessage(new ThermobathTogglePowerMessage());
        _window.OnSetpointChanged += setpoint => SendPredictedMessage(new ThermobathSetpointChangedMessage(setpoint));
        _window.OnModeChanged += mode => SendPredictedMessage(new ThermobathModeChangedMessage(mode));

        EntMan.TryGetComponent(Owner, out _thermoregulator);
        UpdateWindow();
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        UpdatePower();
    }

    public override void Update()
    {
        UpdateWindow();
    }

    private void UpdateWindow()
    {
        if (_window == null)
            return;

        UpdatePower();
        UpdateThermobath(_window);

        if (_thermoregulator == null)
            return;

        _window.SetMode(_thermoregulator.Mode);

        _window.SetTemperatureLimits(_thermoregulator.MinTemperature, _thermoregulator.MaxTemperature);
        UpdateThermoregulator(_window, _thermoregulator);
    }

    private void UpdatePower()
    {
        if (_window == null)
            return;

        SharedApcPowerReceiverComponent? receiver = null;
        if (!_power.ResolveApc(Owner, ref receiver))
        {
            _window.SetPowerSwitchState(true);
            _window.SetPowered(true);
            return;
        }

        _window.SetPowerSwitchState(!receiver.PowerDisabled);
        _window.SetPowered(receiver.Powered);
    }

    private void UpdateThermobath(ThermobathMenu window)
    {
        var beaker = _itemSlots.GetItemOrNull(Owner, ThermobathComponent.BeakerSlotId);
        window.SetBeakerPresent(beaker != null);

        if (beaker != null &&
            _solutions.TryGetFitsInDispenser(beaker.Value, out _, out var solution))
        {
            window.SetSolutionTemperature(solution.Temperature);
            return;
        }

        window.SetSolutionTemperature(null);
    }

    private static void UpdateThermoregulator(ThermobathMenu window, ThermoregulatorComponent comp)
    {
        window.SetCurrentTemperature(comp.Temperature);
        window.SetSetpoint(comp.Setpoint);
        window.SetActiveMode(comp.ActiveMode);
    }
}
