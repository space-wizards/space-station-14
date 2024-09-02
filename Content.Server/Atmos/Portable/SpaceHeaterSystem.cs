using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos.Piping.Portable.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.Portable;

public sealed class SpaceHeaterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceHeaterComponent, ActivatableUIOpenAttemptEvent>(OnUIActivationAttempt);
        SubscribeLocalEvent<SpaceHeaterComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpened);

        SubscribeLocalEvent<SpaceHeaterComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
        SubscribeLocalEvent<SpaceHeaterComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<SpaceHeaterComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<SpaceHeaterComponent, SpaceHeaterChangeModeMessage>(OnModeChanged);
        SubscribeLocalEvent<SpaceHeaterComponent, SpaceHeaterChangePowerLevelMessage>(OnPowerLevelChanged);
        SubscribeLocalEvent<SpaceHeaterComponent, SpaceHeaterChangeTemperatureMessage>(OnTemperatureChanged);
        SubscribeLocalEvent<SpaceHeaterComponent, SpaceHeaterToggleMessage>(OnToggle);
    }

    private void OnInit(EntityUid uid, SpaceHeaterComponent spaceHeater, MapInitEvent args)
    {
        if (!TryComp<GasThermoMachineComponent>(uid, out var thermoMachine))
            return;
        thermoMachine.Cp = spaceHeater.HeatingCp;
        thermoMachine.HeatCapacity = spaceHeater.PowerConsumption;
    }

    private void OnBeforeOpened(EntityUid uid, SpaceHeaterComponent spaceHeater, BeforeActivatableUIOpenEvent args)
    {
        DirtyUI(uid, spaceHeater);
    }

    private void OnUIActivationAttempt(EntityUid uid, SpaceHeaterComponent spaceHeater, ActivatableUIOpenAttemptEvent args)
    {
        if (!Comp<TransformComponent>(uid).Anchored)
        {
            _popup.PopupEntity(Loc.GetString("comp-space-heater-unanchored", ("device", Loc.GetString("comp-space-heater-device-name"))), uid, args.User);
            args.Cancel();
        }
    }

    private void OnDeviceUpdated(EntityUid uid, SpaceHeaterComponent spaceHeater, ref AtmosDeviceUpdateEvent args)
    {
        if (!_power.IsPowered(uid)
            || !TryComp<GasThermoMachineComponent>(uid, out var thermoMachine))
        {
            return;
        }

        UpdateAppearance(uid);

        // If in automatic temperature mode, check if we need to adjust the heat exchange direction
        if (spaceHeater.Mode == SpaceHeaterMode.Auto)
        {
            var environment = _atmosphereSystem.GetContainingMixture(uid, args.Grid, args.Map);
            if (environment == null)
                return;

            if (environment.Temperature <= thermoMachine.TargetTemperature - (thermoMachine.TemperatureTolerance + spaceHeater.AutoModeSwitchThreshold))
            {
                thermoMachine.Cp = spaceHeater.HeatingCp;
            }
            else if (environment.Temperature >= thermoMachine.TargetTemperature + (thermoMachine.TemperatureTolerance + spaceHeater.AutoModeSwitchThreshold))
            {
                thermoMachine.Cp = spaceHeater.CoolingCp;
            }
        }
    }

    private void OnPowerChanged(EntityUid uid, SpaceHeaterComponent spaceHeater, ref PowerChangedEvent args)
    {
        UpdateAppearance(uid);
        DirtyUI(uid, spaceHeater);
    }

    private void OnToggle(EntityUid uid, SpaceHeaterComponent spaceHeater, SpaceHeaterToggleMessage args)
    {
        ApcPowerReceiverComponent? powerReceiver = null;
        if (!Resolve(uid, ref powerReceiver))
            return;

        _power.TogglePower(uid);

        UpdateAppearance(uid);
        DirtyUI(uid, spaceHeater);
    }

    private void OnTemperatureChanged(EntityUid uid, SpaceHeaterComponent spaceHeater, SpaceHeaterChangeTemperatureMessage args)
    {
        if (!TryComp<GasThermoMachineComponent>(uid, out var thermoMachine))
            return;

        thermoMachine.TargetTemperature = float.Clamp(thermoMachine.TargetTemperature + args.Temperature, thermoMachine.MinTemperature, thermoMachine.MaxTemperature);

        UpdateAppearance(uid);
        DirtyUI(uid, spaceHeater);
    }

    private void OnModeChanged(EntityUid uid, SpaceHeaterComponent spaceHeater, SpaceHeaterChangeModeMessage args)
    {
        if (!TryComp<GasThermoMachineComponent>(uid, out var thermoMachine))
            return;

        spaceHeater.Mode = args.Mode;

        if (spaceHeater.Mode == SpaceHeaterMode.Heat)
            thermoMachine.Cp = spaceHeater.HeatingCp;
        else if (spaceHeater.Mode == SpaceHeaterMode.Cool)
            thermoMachine.Cp = spaceHeater.CoolingCp;

        DirtyUI(uid, spaceHeater);
    }

    private void OnPowerLevelChanged(EntityUid uid, SpaceHeaterComponent spaceHeater, SpaceHeaterChangePowerLevelMessage args)
    {
        if (!TryComp<GasThermoMachineComponent>(uid, out var thermoMachine))
            return;

        spaceHeater.PowerLevel = args.PowerLevel;

        switch (spaceHeater.PowerLevel)
        {
            case SpaceHeaterPowerLevel.Low:
                thermoMachine.HeatCapacity = spaceHeater.PowerConsumption / 2;
                break;

            case SpaceHeaterPowerLevel.Medium:
                thermoMachine.HeatCapacity = spaceHeater.PowerConsumption;
                break;

            case SpaceHeaterPowerLevel.High:
                thermoMachine.HeatCapacity = spaceHeater.PowerConsumption * 2;
                break;
        }

        DirtyUI(uid, spaceHeater);
    }

    private void DirtyUI(EntityUid uid, SpaceHeaterComponent? spaceHeater)
    {
        if (!Resolve(uid, ref spaceHeater)
            || !TryComp<GasThermoMachineComponent>(uid, out var thermoMachine)
            || !TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver))
        {
            return;
        }
        _userInterfaceSystem.SetUiState(uid, SpaceHeaterUiKey.Key,
            new SpaceHeaterBoundUserInterfaceState(spaceHeater.MinTemperature, spaceHeater.MaxTemperature, thermoMachine.TargetTemperature, !powerReceiver.PowerDisabled, spaceHeater.Mode, spaceHeater.PowerLevel));
    }

    private void UpdateAppearance(EntityUid uid)
    {
        if (!_power.IsPowered(uid) || !TryComp<GasThermoMachineComponent>(uid, out var thermoMachine))
        {
            _appearance.SetData(uid, SpaceHeaterVisuals.State, SpaceHeaterState.Off);
            return;
        }

        if (thermoMachine.LastEnergyDelta > 0)
        {
            _appearance.SetData(uid, SpaceHeaterVisuals.State, SpaceHeaterState.Heating);
        }
        else if (thermoMachine.LastEnergyDelta < 0)
        {
            _appearance.SetData(uid, SpaceHeaterVisuals.State, SpaceHeaterState.Cooling);
        }
        else
        {
            _appearance.SetData(uid, SpaceHeaterVisuals.State, SpaceHeaterState.StandBy);
        }
    }
}
