using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Toggleable;
using Content.Shared.UserInterface;

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedGasPressurePumpSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private readonly ToggleableSystem _toggleableSystem = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;

    private static ToggleableEnabledEvent _enabledEvent = new();
    private static ToggleableDisabledEvent _disabledEvent = new();

    // TODO: Check enabled for activatableUI
    // TODO: Add activatableUI to it.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasPressurePumpComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasPressurePumpComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<GasPressurePumpComponent, GasPressurePumpChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
        SubscribeLocalEvent<GasPressurePumpComponent, GasPressurePumpToggleStatusMessage>(OnToggleStatusMessage);

        SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceDisabledEvent>(OnPumpLeaveAtmosphere);
        SubscribeLocalEvent<GasPressurePumpComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<GasPressurePumpComponent, ToggleableEnabledEvent>(OnPumpToggledOn);
        SubscribeLocalEvent<GasPressurePumpComponent, ToggleableDisabledEvent>(OnPumpToggledOff);
    }

    private void OnExamined(Entity<GasPressurePumpComponent> ent, ref ExaminedEvent args)
    {
        if (!Transform(ent).Anchored)
            return;

        if (Loc.TryGetString("gas-pressure-pump-system-examined",
                out var str,
                ("statusColor", "lightblue"), // TODO: change with pressure?
                ("pressure", ent.Comp.TargetPressure)
            ))
        {
            args.PushMarkup(str);
        }
    }

    private void OnInit(Entity<GasPressurePumpComponent> ent, ref ComponentInit args)
    {
        _toggleableSystem.SetEnabled(ent.Owner, ent.Comp.InitialEnabled, out _);
    }

    private void OnPowerChanged(Entity<GasPressurePumpComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<GasPressurePumpComponent, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        var pumpOn = _toggleableSystem.IsEnabled(ent.Owner) && _receiver.IsPowered(ent.Owner);
        _appearance.SetData(ent, PumpVisuals.Enabled, pumpOn, ent.Comp2);
    }

    private void OnToggleStatusMessage(Entity<GasPressurePumpComponent> ent, ref GasPressurePumpToggleStatusMessage args)
    {
        _toggleableSystem.SetEnabled(ent.Owner, args.Enabled);
        _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(ent):device} to {args.Enabled}");

        Dirty(ent);
        UpdateUi(ent);
        UpdateAppearance(ent);
    }

    private void OnOutputPressureChangeMessage(Entity<GasPressurePumpComponent> ent, ref GasPressurePumpChangeOutputPressureMessage args)
    {
        ent.Comp.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);
        _adminLogger.Add(LogType.AtmosPressureChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(ent):device} to {args.Pressure}kPa");
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnPumpLeaveAtmosphere(Entity<GasPressurePumpComponent> ent, ref AtmosDeviceDisabledEvent args)
    {
        _toggleableSystem.SetEnabled(ent.Owner, false);
        UserInterfaceSystem.CloseUi(ent.Owner, GasPressurePumpUiKey.Key);
    }

    protected virtual void UpdateUi(Entity<GasPressurePumpComponent> ent)
    {
    }

    private void OnPumpToggledOn(Entity<GasPressurePumpComponent> ent, ref ToggleableEnabledEvent args)
    {
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void OnPumpToggledOff(Entity<GasPressurePumpComponent> ent, ref ToggleableDisabledEvent args)
    {
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }
}
