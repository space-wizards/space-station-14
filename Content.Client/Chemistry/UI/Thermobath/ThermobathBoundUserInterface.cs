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

        _window.OnPowerChanged += enabled => SendPredictedMessage(new ThermobathPowerChangedMessage(enabled));
        _window.OnSetpointChanged += setpoint => SendPredictedMessage(new ThermobathSetpointChangedMessage(setpoint));
        _window.OnModeChanged += mode => SendPredictedMessage(new ThermobathModeChangedMessage(mode));

        EntMan.TryGetComponent(Owner, out _thermoregulator);
        UpdateWindow();
    }

    public override void Update()
    {
        UpdateWindow();
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        if (_window != null)
            UpdatePower(_window);
    }

    private void UpdateWindow()
    {
        if (_window == null)
            return;

        UpdatePower(_window);
        UpdateThermobath(_window);

        if (_thermoregulator == null)
            return;

        _window.SetMode(_thermoregulator.Mode);

        _window.SetTemperatureLimits(_thermoregulator.MinTemperature, _thermoregulator.MaxTemperature);
        _window.SetCurrentTemperature(_thermoregulator.Temperature);
        _window.SetSetpoint(_thermoregulator.Setpoint);
        _window.SetActiveMode(_thermoregulator.ActiveMode);
    }

    private void UpdatePower(ThermobathMenu window)
    {
        SharedApcPowerReceiverComponent? receiver = null;
        if (!_power.ResolveApc(Owner, ref receiver))
        {
            window.SetPowerSwitchState(true);
            window.SetPowered(true);
            return;
        }

        window.SetPowerSwitchState(!receiver.PowerDisabled);
        window.SetPowered(receiver.Powered);
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
}
