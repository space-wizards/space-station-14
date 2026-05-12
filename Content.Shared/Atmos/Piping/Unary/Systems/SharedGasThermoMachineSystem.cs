using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Atmos.Piping.Unary.Systems;

public abstract class SharedGasThermoMachineSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasThermoMachineComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineToggleMessage>(OnToggleMessage);
        SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineChangeTemperatureMessage>(OnChangeTemperature);
    }

    private void OnExamined(EntityUid uid, GasThermoMachineComponent thermoMachine, ExaminedEvent args)
    {
        if (Loc.TryGetString("gas-thermomachine-system-examined",
                out var str,
                ("machineName", !IsHeater(thermoMachine) ? "freezer" : "heater"),
                ("tempColor", !IsHeater(thermoMachine) ? "deepskyblue" : "red"),
                ("temp", Math.Round(thermoMachine.TargetTemperature, 2))
            ))
        {
            args.PushMarkup(str);
        }
    }

    public bool IsHeater(GasThermoMachineComponent comp)
    {
        return comp.Cp >= 0;
    }

    private void OnToggleMessage(EntityUid uid, GasThermoMachineComponent thermoMachine, GasThermomachineToggleMessage args)
    {
        var powerState = _receiver.TogglePower(uid, user: args.Actor);
        _adminLogger.Add(LogType.AtmosPowerChanged, $"{ToPrettyString(args.Actor)} turned {(powerState ? "On" : "Off")} {ToPrettyString(uid)}");
        DirtyUI(uid, thermoMachine);
    }

    private void OnChangeTemperature(EntityUid uid, GasThermoMachineComponent thermoMachine, GasThermomachineChangeTemperatureMessage args)
    {
        if (IsHeater(thermoMachine))
            thermoMachine.TargetTemperature = MathF.Min(args.Temperature, thermoMachine.MaxTemperature);
        else
            thermoMachine.TargetTemperature = MathF.Max(args.Temperature, thermoMachine.MinTemperature);
        thermoMachine.TargetTemperature = MathF.Max(thermoMachine.TargetTemperature, Atmospherics.TCMB);
        _adminLogger.Add(LogType.AtmosTemperatureChanged, $"{ToPrettyString(args.Actor)} set temperature on {ToPrettyString(uid)} to {thermoMachine.TargetTemperature}");
        Dirty(uid, thermoMachine);
        DirtyUI(uid, thermoMachine);
    }

    protected virtual void DirtyUI(EntityUid uid, GasThermoMachineComponent? thermoMachine, UserInterfaceComponent? ui=null) {}
}
