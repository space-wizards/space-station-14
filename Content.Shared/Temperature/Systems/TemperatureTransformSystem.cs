using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Temperature.Components;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// Handles replacing <see cref="TemperatureTransformComponent"/>'s when their temperature is reached.
/// </summary>
public sealed partial class TemperatureTransformSystem : EntitySystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;

    [SubscribeLocalEvent]
    private void OnTemperatureChanged(Entity<TemperatureTransformComponent> ent, ref TemperatureChangedEvent args)
    {
        var curTemp = args.CurrentTemperature;

        foreach (var entry in ent.Comp.Entries)
        {
            var minTemp = entry.TemperatureRange.X;
            var maxTemp = entry.TemperatureRange.Y;

            if (curTemp < minTemp || curTemp > maxTemp)
                continue;

            ReplaceItem(ent, entry.Table);
            break;
        }
    }

    /// <summary>
    /// Deletes <see cref="uid"/> and then spawns <see cref="table"/>
    /// </summary>
    /// <param name="uid">The entity to delete</param>
    /// <param name="table">The table to spawn</param>
    private void ReplaceItem(EntityUid uid, EntityTableSelector table)
    {
        var spawns = _entityTable.GetSpawns(table);
        foreach (var spawn in spawns)
        {
            PredictedSpawnAtPosition(spawn, Transform(uid).Coordinates);
        }

        QueueDel(uid);
    }
}
