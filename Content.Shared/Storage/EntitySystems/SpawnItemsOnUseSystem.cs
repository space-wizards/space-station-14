using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SpawnItemsOnUseSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(EntityUid uid, SpawnItemsOnUseComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // If starting with zero or fewer uses, this component is a no-op
        if (component.Uses <= 0)
            return;

        var xform = Transform(args.User);
        var spawnEntities = GetSpawns(component.Items, _random);

        EntityUid? entityToPlaceInHands =  null;
        foreach (var proto in spawnEntities)
        {
            var spawned = SpawnNextToOrDrop(proto, args.User, xform);
            _hands.TryPickupAnyHand(args.User, spawned);
            entityToPlaceInHands ??= spawned;

            _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.User)} used {ToPrettyString(uid)} which spawned {ToPrettyString(entityToPlaceInHands.Value)}");
        }

        // The entity is often deleted, so play the sound at its position rather than parenting
        if (component.Sound != null)
            _audio.PlayPvs(component.Sound, xform.Coordinates);

        component.Uses--;

        // Delete entity only if component was successfully used
        if (component.Uses <= 0)
        {
            var holding = _hands.IsHolding(args.User, uid, out var hand);
            // Don't delete the entity in the event bus, so we queue it for deletion.
            // We need the free hand for the new item, so we send it to nullspace.
            _transform.DetachEntity(uid, Transform(uid));
            PredictedQueueDel(uid);

            if (holding && entityToPlaceInHands is not null)
                _hands.TryPickup(args.User, entityToPlaceInHands.Value, hand);
        }

        args.Handled = true;
    }
}
