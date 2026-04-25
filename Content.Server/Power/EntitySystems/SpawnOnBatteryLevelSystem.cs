using Content.Server.Power.Components;
using Content.Shared.EntityTable;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Power.EntitySystems;

public sealed class SpawnOnBatteryLevelSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = null!;
    [Dependency] private readonly EntityTableSystem _entityTable =  null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnBatteryLevelComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SpawnOnBatteryLevelComponent, ChargeChangedEvent>(OnBatteryChargeChange);
    }

    private void OnComponentInit(Entity<SpawnOnBatteryLevelComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.Charge != 0f || !TryComp<BatteryComponent>(entity, out var battery))
            return;

        entity.Comp.Charge = battery.MaxCharge;
    }

    private void OnBatteryChargeChange(Entity<SpawnOnBatteryLevelComponent> entity, ref ChargeChangedEvent args)
    {
        // only cares about battery charging, not discharging
        if (args.Delta < 0)
            return;

        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        if (battery.LastCharge < entity.Comp.Charge)
            return;

        var spawns = entity.Comp.Proto == null ? _entityTable.GetSpawns(entity.Comp.Table) : new List<EntProtoId>{entity.Comp.Proto.Value};
        foreach (var spawn in spawns)
        {
            Spawn(spawn, Transform(entity).Coordinates);
        }

        _battery.ChangeCharge((entity, battery), -entity.Comp.Charge);
    }
}
