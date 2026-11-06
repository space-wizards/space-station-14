using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Atmos.Piping.Unary.Systems;

public abstract partial class SharedGasThermoMachineSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;

    [SubscribeLocalEvent]
    private void OnExamined(Entity<GasThermoMachineComponent> ent, ref ExaminedEvent args)
    {
        if (Loc.TryGetString("gas-thermomachine-system-examined",
                out var str,
                ("machineName", Name(ent.Owner)),
                ("tempColor", !IsHeater(ent.Comp) ? "deepskyblue" : "red"),
                ("temp", Math.Round(ent.Comp.TargetTemperature, 2))
            ))
        {
            args.PushMarkup(str);
        }
    }

    public bool IsHeater(GasThermoMachineComponent comp)
    {
        return comp.Cp >= 0;
    }

    [SubscribeLocalEvent]
    private void OnToggleMessage(Entity<GasThermoMachineComponent> ent, ref GasThermomachineToggleMessage args)
    {
        var powerState = _receiver.TogglePower(ent.Owner, user: args.Actor);
        _adminLogger.Add(LogType.AtmosPowerChanged, $"{ToPrettyString(args.Actor)} turned {(powerState ? "On" : "Off")} {ToPrettyString(ent)}");
        DirtyUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnChangeTemperature(Entity<GasThermoMachineComponent> ent, ref GasThermomachineChangeTemperatureMessage args)
    {
        var thermoMachine = ent.Comp;
        if (IsHeater(thermoMachine))
            thermoMachine.TargetTemperature = MathF.Min(args.Temperature, thermoMachine.MaxTemperature);
        else
            thermoMachine.TargetTemperature = MathF.Max(args.Temperature, thermoMachine.MinTemperature);
        thermoMachine.TargetTemperature = MathF.Max(thermoMachine.TargetTemperature, Atmospherics.TCMB);
        _adminLogger.Add(LogType.AtmosTemperatureChanged, $"{ToPrettyString(args.Actor)} set temperature on {ToPrettyString(ent.Owner)} to {thermoMachine.TargetTemperature}");
        Dirty(ent);
        DirtyUI(ent);
    }

    protected virtual void DirtyUI(Entity<GasThermoMachineComponent> ent, UserInterfaceComponent? ui = null)
    {
    }
}
