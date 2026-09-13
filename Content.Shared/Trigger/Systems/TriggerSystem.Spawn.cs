using Content.Shared.EntityTable;
using Content.Shared.GameTicking;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    [SubscribeLocalEvent]
    private void OnSpawnInit(Entity<TriggerOnSpawnComponent> ent, ref MapInitEvent args)
    {
        Trigger(ent.Owner, null, ent.Comp.KeyOut);
    }

    [SubscribeLocalEvent]
    private void OnPlayerSpawn(Entity<TriggerOnPlayerSpawnCompleteComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        Trigger(ent.Owner, null, ent.Comp.KeyOut);
    }

    /// <summary>
    /// Helper function to combine HandleSpawnOnTrigger and HandleSpawnTableOnTrigger.
    /// </summary>
    /// <param name="target">The entity to spawn attached to or at the feet of.</param>
    /// <param name="proto">The entity to spawn.</param>
    /// <param name="useMapCoords">If true, spawn at target's MapCoordinates. If false, spawn attached to target.</param>
    /// <param name="predicted">Whether to use predicted spawning.</param>
    public void SpawnTriggerHelper(Entity<TransformComponent> target, EntProtoId proto, bool useMapCoords, bool predicted)
    {
        if (useMapCoords)
        {
            var mapCoords = _transform.GetMapCoordinates(target);
            if (predicted)
                EntityManager.PredictedSpawn(proto, mapCoords);
            else if (_net.IsServer)
                Spawn(proto, mapCoords);
        }

        else
        {
            var coords = target.Comp.Coordinates;
            if (!coords.IsValid(EntityManager))
                return;

            if (predicted)
                PredictedSpawnAttachedTo(proto, coords);
            else if (_net.IsServer)
                SpawnAttachedTo(proto, coords);
        }
    }
}

public sealed partial class SpawnOnTriggerSystem : XOnTriggerSystem<SpawnOnTriggerComponent>
{
    protected override void OnTrigger(Entity<SpawnOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var xform = Transform(target);
        Trigger.SpawnTriggerHelper((target, xform), ent.Comp.Proto, ent.Comp.UseMapCoords, ent.Comp.Predicted);

        args.Handled = true;
    }
}

public sealed partial class SpawnEntityTableOnTriggerSystem : XOnTriggerSystem<SpawnEntityTableOnTriggerComponent>
{
    [Dependency] private EntityTableSystem _entityTable = default!;

    protected override void OnTrigger(Entity<SpawnEntityTableOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var xform = Transform(target);
        var spawns = _entityTable.GetSpawns(ent.Comp.Table);
        foreach (var proto in spawns)
        {
            Trigger.SpawnTriggerHelper((target, xform), proto, ent.Comp.UseMapCoords, ent.Comp.Predicted);
        }

        args.Handled = true;
    }
}

public sealed partial class DeleteOnTriggerSystem : XOnTriggerSystem<DeleteOnTriggerComponent>
{
    protected override void OnTrigger(Entity<DeleteOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        PredictedQueueDel(target);
        args.Handled = true;
    }
}
