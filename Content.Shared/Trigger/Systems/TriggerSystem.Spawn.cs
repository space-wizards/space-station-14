using Content.Shared.GameTicking;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeSpawn()
    {
        SubscribeLocalEvent<TriggerOnSpawnComponent, MapInitEvent>(OnSpawnInit);
        SubscribeLocalEvent<TriggerOnPlayerSpawnCompleteComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawn);

        SubscribeLocalEvent<SpawnOnTriggerComponent, TriggerEvent>(HandleSpawnOnTrigger);
        SubscribeLocalEvent<SpawnEntityTableOnTriggerComponent, TriggerEvent>(HandleSpawnTableOnTrigger);
        SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteOnTrigger);
    }

    private void OnSpawnInit(Entity<TriggerOnSpawnComponent> ent, ref MapInitEvent args)
    {
        Trigger(ent.Owner, null, ent.Comp.KeyOut);
    }

    private void OnPlayerSpawn(Entity<TriggerOnPlayerSpawnCompleteComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        Trigger(ent.Owner, null, ent.Comp.KeyOut);
    }

    private void HandleSpawnOnTrigger(Entity<SpawnOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        var xform = Transform(target.Value);
        SpawnTriggerHelper((target.Value, xform), ent.Comp.Proto, ent.Comp.UseMapCoords, ent.Comp.Predicted);
    }

    private void HandleSpawnTableOnTrigger(Entity<SpawnEntityTableOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        var xform = Transform(target.Value);
        var spawns = _entityTable.GetSpawns(ent.Comp.Table);
        foreach (var proto in spawns)
        {
            SpawnTriggerHelper((target.Value, xform), proto, ent.Comp.UseMapCoords, ent.Comp.Predicted);
        }
    }

    /// <summary>
    /// Helper function to combine HandleSpawnOnTrigger and HandleSpawnTableOnTrigger.
    /// </summary>
    /// <param name="target">The entity to spawn attached to or at the feet of.</param>
    /// <param name="proto">The entity to spawn.</param>
    /// <param name="useMapCoords">If true, spawn at target's MapCoordinates. If false, spawn attached to target.</param>
    /// <param name="predicted">Whether to use predicted spawning.</param>
    private void SpawnTriggerHelper(Entity<TransformComponent> target, EntProtoId proto, bool useMapCoords, bool predicted)
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

    private void HandleDeleteOnTrigger(Entity<DeleteOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        PredictedQueueDel(target);
        args.Handled = true;
    }
}
