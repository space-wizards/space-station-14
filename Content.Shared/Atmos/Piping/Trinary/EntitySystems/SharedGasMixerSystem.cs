using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Examine;

namespace Content.Shared.Atmos.Piping.Trinary.EntitySystems;

public abstract partial class SharedGasMixerSystem : EntitySystem
{
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
}
