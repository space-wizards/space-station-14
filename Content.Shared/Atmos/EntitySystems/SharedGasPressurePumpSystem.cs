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

        SubscribeLocalEvent<GasPressurePumpComponent, ToggleableEnabledEvent>(OnPumpToggledEnabled);
        SubscribeLocalEvent<GasPressurePumpComponent, ToggleableDisabledEvent>(OnPumpToggledDisabled);
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
        ent.Comp.ToggleableComponent = Comp<ToggleableComponent>(ent);
        ent.Comp.ToggleableComponent.Enabled = ent.Comp.DefaultEnabled;
        UpdateAppearance(ent);
    }

    private void OnPowerChanged(Entity<GasPressurePumpComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<GasPressurePumpComponent, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        var pumpOn = ent.Comp1.ToggleableComponent.Enabled && _receiver.IsPowered(ent.Owner);
        _appearance.SetData(ent, PumpVisuals.Enabled, pumpOn, ent.Comp2);
    }

    private void OnToggleStatusMessage(Entity<GasPressurePumpComponent> ent, ref GasPressurePumpToggleStatusMessage args)
    {
        if (args.Enabled)
            RaiseLocalEvent(ent, ref _enabledEvent);
        else
            RaiseLocalEvent(ent, ref _disabledEvent);

        _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(ent):device} to {args.Enabled}");
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
        RaiseLocalEvent(ent, ref _disabledEvent);
        UserInterfaceSystem.CloseUi(ent.Owner, GasPressurePumpUiKey.Key);
    }

    protected virtual void UpdateUi(Entity<GasPressurePumpComponent> ent)
    {
    }

    private void OnPumpToggledEnabled(Entity<GasPressurePumpComponent> ent, ref ToggleableEnabledEvent args)
    {
        ent.Comp.ToggleableComponent.Enabled = true;
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void OnPumpToggledDisabled(Entity<GasPressurePumpComponent> ent, ref ToggleableDisabledEvent args)
    {
        ent.Comp.ToggleableComponent.Enabled = false;
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }
}
