using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Database;

namespace Content.Shared.Atmos.Piping.Trinary.EntitySystems;

public abstract partial class SharedGasMixerSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasMixerComponent, GasMixerToggleStatusMessage>(OnToggleStatusMessage);
        SubscribeLocalEvent<GasMixerComponent, GasMixerChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
        SubscribeLocalEvent<GasMixerComponent, GasMixerChangeNodePercentageMessage>(OnChangeNodePercentageMessage);
    }
    protected virtual void UpdateUi(Entity<GasMixerComponent> ent)
    {
    }

    protected void UpdateAppearance(Entity<GasMixerComponent> ent)
    {
        _appearance.SetData(ent, FilterVisuals.Enabled, ent.Comp.Enabled);
    }

    private void OnToggleStatusMessage(Entity<GasMixerComponent> ent, ref GasMixerToggleStatusMessage args)
    {
        ent.Comp.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(ent.Owner):device} to {args.Enabled}");
        Dirty(ent);
        UpdateUi(ent);
        UpdateAppearance(ent);
    }

    private void OnOutputPressureChangeMessage(Entity<GasMixerComponent> ent, ref GasMixerChangeOutputPressureMessage args)
    {
        ent.Comp.TargetPressure = Math.Clamp(args.Pressure, 0f, ent.Comp.MaxTargetPressure);
        _adminLogger.Add(LogType.AtmosPressureChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(ent.Owner):device} to {ent.Comp.TargetPressure}kPa");
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnChangeNodePercentageMessage(Entity<GasMixerComponent> ent,
        ref GasMixerChangeNodePercentageMessage args)
    {
        float nodeOne = Math.Clamp(args.NodeOne, 0f, 100.0f) / 100.0f;
        ent.Comp.InletOneConcentration = nodeOne;
        ent.Comp.InletTwoConcentration = 1.0f - ent.Comp.InletOneConcentration;
        _adminLogger.Add(LogType.AtmosRatioChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the ratio on {ToPrettyString(ent.Owner):device} to {ent.Comp.InletOneConcentration}:{ent.Comp.InletTwoConcentration}");
        Dirty(ent);
        UpdateUi(ent);
    }
}
