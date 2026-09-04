using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// Contains logic related to the <see cref="SpawnItemsOnUseComponent"/>.
/// </summary>
public abstract partial class SpawnItemsOnUseSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<SpawnItemsOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var spawnEntities = GetSpawns(ent.Comp.Items, SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent)));

        ent.Comp.Uses--;
        var remove = false;
        if (ent.Comp.Uses <= 0)
        {
            _hands.TryDrop(args.User, ent);
            remove = true;
        }

        foreach (var proto in spawnEntities)
        {
            var spawn = PredictedSpawnNextToOrDrop(proto, args.User);

            _hands.TryPickupAnyHand(args.User, spawn);
            _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.User)} used {ToPrettyString(ent)} which spawned {ToPrettyString(spawn)}");
        }

        // The entity is often deleted, so play the sound at its position rather than parenting
        if (ent.Comp.Sound != null)
            _audio.PlayPredicted(ent.Comp.Sound, ent, args.User);

        if (remove)
            PredictedQueueDel(ent);

        args.Handled = true;
    }
}
