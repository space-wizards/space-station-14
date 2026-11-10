using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Database;
using Content.Shared.Examine;

namespace Content.Shared.Atmos.Piping.Trinary.EntitySystems;

public abstract partial class SharedGasMixerSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasMixerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<GasMixerComponent> ent, ref ExaminedEvent args)
    {
        if (Loc.TryGetString("gas-pressure-pump-system-examined",
                out var transferPressureStr,
                ("statusColor", "lightblue"),
                ("pressure", ent.Comp.TargetPressure)
            ))
        {
            args.PushMarkup(transferPressureStr);
        }

        if (Loc.TryGetString("comp-gas-mixer-ratio-examine",
                out var sidePortRatioStr,
                ("statusColor", "lightblue"),
                ("sidePortRatio", ent.Comp.InletTwoConcentration.ToString("0.##%"))
            ))
        {
            args.PushMarkup(sidePortRatioStr);
        }
    }

    [SubscribeLocalEvent]
    private void OnToggleStatusMessage(Entity<GasMixerComponent> ent, ref GasMixerToggleStatusMessage args)
    {
        ent.Comp.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(ent.Owner):device} to {args.Enabled}");

        DirtyField(ent.Owner, ent.Comp, nameof(GasMixerComponent.Enabled));
        UpdateUi(ent);
        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnOutputPressureChangeMessage(Entity<GasMixerComponent> ent, ref GasMixerChangeOutputPressureMessage args)
    {
        ent.Comp.TargetPressure = Math.Clamp(args.Pressure, 0f, ent.Comp.MaxTargetPressure);
        _adminLogger.Add(LogType.AtmosPressureChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(ent.Owner):device} to {ent.Comp.TargetPressure}kPa");

        DirtyField(ent.Owner, ent.Comp, nameof(GasMixerComponent.TargetPressure));
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnChangeNodePercentageMessage(Entity<GasMixerComponent> ent,
        ref GasMixerChangeNodePercentageMessage args)
    {
        var nodeOne = Math.Clamp(args.NodeOne, 0f, 100.0f) / 100.0f;
        ent.Comp.InletOneConcentration = nodeOne;
        ent.Comp.InletTwoConcentration = 1.0f - ent.Comp.InletOneConcentration;
        _adminLogger.Add(LogType.AtmosRatioChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the ratio on {ToPrettyString(ent.Owner):device} to {ent.Comp.InletOneConcentration}:{ent.Comp.InletTwoConcentration}");

        DirtyFields(ent.Owner, ent.Comp, null, nameof(GasMixerComponent.InletOneConcentration), nameof(GasMixerComponent.InletTwoConcentration));
        UpdateUi(ent);
    }

    protected void UpdateAppearance(Entity<GasMixerComponent> ent)
    {
        _appearance.SetData(ent, FilterVisuals.Enabled, ent.Comp.Enabled);
    }

    protected virtual void UpdateUi(Entity<GasMixerComponent> ent)
    {
    }
}
