using Content.Server.Power.Components;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
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

        if (entity.Comp.Proto == null)
            SpawnFromEntityTable(entity, entity.Comp.Table);
        else
            SpawnFromProto(entity, entity.Comp.Proto.Value);

        _battery.ChangeCharge((entity, battery), -entity.Comp.Charge);
    }

    private void SpawnFromEntityTable(EntityUid entity, EntityTableSelector? table)
    {
        var spawns = _entityTable.GetSpawns(table);
        foreach (var spawn in spawns)
        {
            Spawn(spawn, Transform(entity).Coordinates);
        }
    }

    private void SpawnFromProto(EntityUid entity, EntProtoId proto)
    {
        Spawn(proto, Transform(entity).Coordinates);
    }
}
