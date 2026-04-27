using Content.Server.Power.Components;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Power.EntitySystems;

public sealed class SpawnOnBatteryFullSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnBatteryFullComponent, BatteryStateChangedEvent>(OnBatteryStateChange);
    }

    private void OnBatteryStateChange(Entity<SpawnOnBatteryFullComponent> entity, ref BatteryStateChangedEvent args)
    {
        if (args.NewState != BatteryState.Full)
            return;

        if (entity.Comp.Proto == null)
            SpawnFromEntityTable(entity, entity.Comp.Table);
        else
            SpawnFromProto(entity, entity.Comp.Proto.Value);

        _battery.SetCharge(entity.Owner, 0);
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
