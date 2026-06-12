using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Examine;

namespace Content.Shared.Atmos.Piping.Binary.Systems;

public abstract partial class SharedGasHeatPumpSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasHeatPumpComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<GasHeatPumpComponent> ent, ref ExaminedEvent args)
    {
        if (!Transform(ent).Anchored || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gas-heat-pump-system-examined",
            ("statusColor", "lightblue"),
            ("temp", $"{ent.Comp.TargetTemperature:0.##}")));

        if (ent.Comp.Blocked)
            args.PushMarkup(Loc.GetString("gas-heat-pump-system-examined-blocked"));

        if (ent.Comp.TemperatureLocked)
            args.PushMarkup(Loc.GetString("gas-heat-pump-system-examined-locked"));
    }
}
