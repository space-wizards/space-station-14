using Content.Server.Power.Components;
using Content.Shared.EntityTable;
using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed class SpawnOnBatteryLevelSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = null!;
    [Dependency] private readonly EntityTableSystem _entityTable =  null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnBatteryLevelComponent, ChargeChangedEvent>(OnBatteryChargeChange);
    }

    private void OnBatteryChargeChange(Entity<SpawnOnBatteryLevelComponent> entity, ref ChargeChangedEvent args)
    {
        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        if (battery.LastCharge < entity.Comp.Charge)
            return;

        var spawns = _entityTable.GetSpawns(entity.Comp.Table);
        foreach (var spawn in spawns)
        {
            Spawn(spawn, Transform(entity).Coordinates);
        }

        _battery.ChangeCharge((entity, battery), -entity.Comp.Charge);
    }
}
