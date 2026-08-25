using Content.Server.Cargo.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Server.Storage.EntitySystems;

/// <summary>
///
/// </summary>
public sealed partial class SpawnItemsOnUseSystem : EntitySystem
{
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void CalculatePrice(Entity<SpawnItemsOnUseComponent> ent, ref PriceCalculationEvent args)
    {
        var ungrouped = CollectOrGroups(ent.Comp.Items, out var orGroups);

        foreach (var entry in ungrouped)
        {
            var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

            // Calculate the average price of the possible spawned items
            args.Price += _pricing.GetPrice(protUid) * entry.SpawnProbability * entry.GetAmount(getAverage: true);

            Del(protUid);
        }

        foreach (var group in orGroups)
        {
            foreach (var entry in group.Entries)
            {
                var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

                // Calculate the average price of the possible spawned items
                args.Price += _pricing.GetPrice(protUid) *
                              (entry.SpawnProbability / group.CumulativeProbability) *
                              entry.GetAmount(getAverage: true);

                Del(protUid);
            }
        }

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<SpawnItemsOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // If starting with zero or fewer uses, this component is a no-op
        if (ent.Comp.Uses <= 0)
            return;

        var xform = Transform(args.User);
        var spawnEntities = GetSpawns(ent.Comp.Items, _random);

        var spawned = new List<EntityUid>();
        foreach (var proto in spawnEntities)
        {
            var spawn = (PredictedSpawnNextToOrDrop(proto, args.User, xform));
            spawned.Add(spawn);

            _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.User)} used {ToPrettyString(ent)} which spawned {ToPrettyString(spawn)}");
        }

        // The entity is often deleted, so play the sound at its position rather than parenting
        if (ent.Comp.Sound != null)
            _audio.PlayPvs(ent.Comp.Sound, xform.Coordinates);

        ent.Comp.Uses--;

        // Delete entity only if component was successfully used
        if (ent.Comp.Uses <= 0)
        {
            _hands.IsHolding(args.User, ent, out var hand);
            // Don't delete the entity in the event bus, so we queue it for deletion.
            // We need the free hand for the new item, so we send it to nullspace.
            _transform.DetachEntity(ent, Transform(ent));
            QueueDel(ent);

            if (spawned.Count != 0)
            {
                _hands.TryPickup(args.User, spawned[0], hand);
                spawned.Remove(spawned[0]);
            }
        }

        foreach (var spawn in spawned)
        {
            _hands.TryPickupAnyHand(args.User, spawn);
        }

        args.Handled = true;
    }
}
