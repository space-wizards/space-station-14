using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class SpawnTableOnUseSystem : EntitySystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    // TODO: This would probably be better off doing GetEstimatedPrice on the spawns rather than spawning every single entity.
    [SubscribeLocalEvent(before: new[] { typeof(PricingSystem) })]
    private void CalculatePrice(Entity<SpawnTableOnUseComponent> ent, ref PriceCalculationEvent args)
    {
        var spawns = _entityTable.AverageSpawns(ent.Comp.Table);

        foreach (var (proto, amount) in spawns)
        {
            var uid = Spawn(proto);

            args.Price += _pricing.GetPrice(uid) * amount * ent.Comp.Uses;

            Del(uid);
        }

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<SpawnTableOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var xform = Transform(ent);
        var spawns = _entityTable.GetSpawns(ent.Comp.Table);
        ent.Comp.Uses--;

        if (ent.Comp.Sound != null)
        {
            _audio.PlayPvs(ent.Comp.Sound, xform.Coordinates); // Entity itself is often being deleted, put it on the parent.
        }

        if (ent.Comp.Uses <= 0)
        {
            // Don't delete the entity in the event bus, so we queue it for deletion.
            // We need the free hand for the new item, so we send it to nullspace.
            _transform.DetachEntity(ent, xform);
            QueueDel(ent);
        }

        foreach (var id in spawns)
        {
            var spawned = SpawnNextToOrDrop(id, args.User); // Entity may be in nullspace, so base it off the user.
            _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.User):user} used {ToPrettyString(ent):spawner} which spawned {ToPrettyString(spawned)}");
            _hands.TryPickupAnyHand(args.User, spawned);
        }

        args.Handled = true;
    }
}
