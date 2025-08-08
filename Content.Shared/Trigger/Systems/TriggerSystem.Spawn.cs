using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{

    private void InitializeSpawn()
    {
        SubscribeLocalEvent<TriggerOnSpawnComponent, MapInitEvent>(OnSpawnInit);

        SubscribeLocalEvent<SpawnOnTriggerComponent, TriggerEvent>(HandleSpawnOnTrigger);
        SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteOnTrigger);
    }

    private void OnSpawnInit(Entity<TriggerOnSpawnComponent> ent, ref MapInitEvent args)
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

        if (ent.Comp.UseMapCoords)
        {
            var mapCoords = _transform.GetMapCoordinates(target.Value, xform);
            if (ent.Comp.Predicted)
                EntityManager.PredictedSpawn(ent.Comp.Proto, mapCoords);
            else if (_net.IsServer)
                Spawn(ent.Comp.Proto, mapCoords);

        }
        else
        {
            var coords = xform.Coordinates;
            if (!coords.IsValid(EntityManager))
                return;

            if (ent.Comp.Predicted)
                PredictedSpawnAttachedTo(ent.Comp.Proto, coords);
            else if (_net.IsServer)
                SpawnAttachedTo(ent.Comp.Proto, coords);

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
