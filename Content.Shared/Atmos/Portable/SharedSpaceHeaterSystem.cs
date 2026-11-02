using Content.Shared.Atmos.Piping.Portable.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;

namespace Content.Shared.Atmos.Portable;

public abstract partial class SharedSpaceHeaterSystem : EntitySystem
{
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] protected SharedUserInterfaceSystem _ui = default!;
    [Dependency] private EntityQuery<GasThermoMachineComponent> _thermoQuery = default!;


    [SubscribeLocalEvent]
    private void OnInit(Entity<SpaceHeaterComponent> ent, ref MapInitEvent args)
    {
        if (!_thermoQuery.TryComp(ent, out var thermoMachine))
            return;

        thermoMachine.Cp = ent.Comp.HeatingCp;
        thermoMachine.HeatCapacity = ent.Comp.PowerConsumption;
        Dirty(ent.Owner, thermoMachine);
    }

    [SubscribeLocalEvent]
    private void OnUIActivationAttempt(Entity<SpaceHeaterComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (Comp<TransformComponent>(ent).Anchored)
            return;

        if (!args.Silent)
            _popup.PopupEntity(Loc.GetString("comp-space-heater-unanchored", ("device", Loc.GetString("comp-space-heater-device-name"))), ent, args.User);

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<SpaceHeaterComponent> ent, ref PowerChangedEvent args)
    {
        if (!_receiver.IsPowered(ent.Owner))
            SetOff(ent);
    }

    [SubscribeLocalEvent]
    private void OnToggle(Entity<SpaceHeaterComponent> ent, ref SpaceHeaterToggleMessage args)
    {
        if (_receiver.TogglePower(ent, user: args.Actor))
            SetOff(ent);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnTemperatureChanged(Entity<SpaceHeaterComponent> ent, ref SpaceHeaterChangeTemperatureMessage args)
    {
        if (!_thermoQuery.TryComp(ent, out var thermoMachine))
            return;

        thermoMachine.TargetTemperature = float.Clamp(thermoMachine.TargetTemperature + args.Temperature,
                                                      ent.Comp.MinTemperature,
                                                      ent.Comp.MaxTemperature);

        Dirty(ent, thermoMachine);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnModeChanged(Entity<SpaceHeaterComponent> ent, ref SpaceHeaterChangeModeMessage args)
    {
        if (!_thermoQuery.TryComp(ent, out var thermoMachine))
            return;

        ent.Comp.Mode = args.Mode;
        Dirty(ent);

        if (ent.Comp.Mode == SpaceHeaterMode.Heat)
            thermoMachine.Cp = ent.Comp.HeatingCp;
        else if (ent.Comp.Mode == SpaceHeaterMode.Cool)
            thermoMachine.Cp = ent.Comp.CoolingCp;
        if (_receiver.IsPowered(ent.Owner))
            SetStandBy(ent); // Set appearance to stand by until the server can figure out if it's actually running or not
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerLevelChanged(Entity<SpaceHeaterComponent> ent, ref SpaceHeaterChangePowerLevelMessage args)
    {
        ent.Comp.PowerLevel = args.PowerLevel;
        Dirty(ent);

        if (!_thermoQuery.TryComp(ent, out var thermoMachine))
            return;

        switch (ent.Comp.PowerLevel)
        {
            case SpaceHeaterPowerLevel.Low:
                thermoMachine.HeatCapacity = ent.Comp.PowerConsumption / 2;
                break;

            case SpaceHeaterPowerLevel.Medium:
                thermoMachine.HeatCapacity = ent.Comp.PowerConsumption;
                break;

            case SpaceHeaterPowerLevel.High:
                thermoMachine.HeatCapacity = ent.Comp.PowerConsumption * 2;
                break;
        }
        UpdateUi(ent);
    }

    protected void UpdateAppearance(EntityUid uid, GasThermoMachineComponent? thermoMachine = null)
    {
        if (!_receiver.IsPowered(uid) || !Resolve(uid, ref thermoMachine, false))
        {
            SetOff(uid);
        }
        else if (thermoMachine.LastEnergyDelta > 0)
        {
            SetHeating(uid);
        }
        else if (thermoMachine.LastEnergyDelta < 0)
        {
            SetCooling(uid);
        }
        else
        {
            SetStandBy(uid);
        }
    }

    protected void SetOff(EntityUid uid)
    {
        _appearance.SetData(uid, SpaceHeaterVisuals.State, SpaceHeaterState.Off);
    }

    protected void SetStandBy(EntityUid uid)
    {
        _appearance.SetData(uid, SpaceHeaterVisuals.State, SpaceHeaterState.StandBy);
    }

    protected void SetHeating(EntityUid uid)
    {
        _appearance.SetData(uid, SpaceHeaterVisuals.State, SpaceHeaterState.Heating);
    }

    protected void SetCooling(EntityUid uid)
    {
        _appearance.SetData(uid, SpaceHeaterVisuals.State, SpaceHeaterState.Cooling);
    }

    protected virtual void UpdateUi(Entity<SpaceHeaterComponent> ent)
    {
    }
}
