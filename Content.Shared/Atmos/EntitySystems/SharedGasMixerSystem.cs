
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;


namespace Content.Server.Atmos.Piping.Trinary.EntitySystems;

public abstract class SharedGasMixerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasMixerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasMixerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<GasMixerComponent, GasMixerChangeNodePercentageMessage>(OnChangeNodePercentageMessage);
        SubscribeLocalEvent<GasMixerComponent, GasMixerChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
        SubscribeLocalEvent<GasMixerComponent, GasMixerToggleStatusMessage>(OnToggleStatusMessage);

        SubscribeLocalEvent<GasMixerComponent, AtmosDeviceDisabledEvent>(OnMixerLeaveAtmosphere);
        SubscribeLocalEvent<GasMixerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(Entity<GasMixerComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnPowerChanged(Entity<GasMixerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<GasMixerComponent, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        var pumpOn = ent.Comp1.Enabled && _receiver.IsPowered(ent.Owner);
        _appearance.SetData(ent, FilterVisuals.Enabled, pumpOn, ent.Comp2);
    }

    private void OnToggleStatusMessage(Entity<GasMixerComponent> ent, ref GasMixerToggleStatusMessage args)
    {
        ent.Comp.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(ent):device} to {args.Enabled}");
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void OnOutputPressureChangeMessage(Entity<GasMixerComponent> ent, ref GasMixerChangeOutputPressureMessage args)
    {
        ent.Comp.TargetPressure = Math.Clamp(args.Pressure, 0f, ent.Comp.MaxTargetPressure);
        _adminLogger.Add(LogType.AtmosPressureChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(ent):device} to {args.Pressure}kPa");
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnChangeNodePercentageMessage(Entity<GasMixerComponent> ent,
        ref GasMixerChangeNodePercentageMessage args)
    {
        var nodeOne = Math.Clamp(args.NodeOne, 0f, 1f);
        _adminLogger.Add(LogType.Unknown, LogImpact.Extreme, $"DEBUG {args.NodeOne} {nodeOne}");
        ent.Comp.InletOneConcentration = nodeOne;
        ent.Comp.InletTwoConcentration = 1.0f - nodeOne;
        _adminLogger.Add(LogType.AtmosRatioChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the ratio on {ToPrettyString(ent):device} to {ent.Comp.InletOneConcentration}:{ent.Comp.InletTwoConcentration}");
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnMixerLeaveAtmosphere(Entity<GasMixerComponent> ent, ref AtmosDeviceDisabledEvent args)
    {
        ent.Comp.Enabled = false;
        Dirty(ent);
        UpdateAppearance(ent);

        UserInterfaceSystem.CloseUi(ent.Owner, GasFilterUiKey.Key);
    }

    private void OnExamined(Entity<GasMixerComponent> ent, ref ExaminedEvent args)
    {
        if (!Transform(ent).Anchored)
            return;

        if (Loc.TryGetString("gas-mixer-system-examined",
                out var str,
                ("statusColor", "lightblue"), // TODO: change with pressure?
                ("pressure", ent.Comp.TargetPressure),
                ("inletOneConcentration", ent.Comp.InletOneConcentration),
                ("inletTwoConcentration", ent.Comp.InletTwoConcentration)
            ))
        {
            args.PushMarkup(str);
        }
    }

    protected virtual void UpdateUi(Entity<GasMixerComponent> ent)
    {
    }
}
