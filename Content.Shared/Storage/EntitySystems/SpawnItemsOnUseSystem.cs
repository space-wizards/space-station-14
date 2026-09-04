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
/// Contains logic related to the <see cref="SpawnItemsOnUseComponent"/>.
/// </summary>
public abstract partial class SpawnItemsOnUseSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<SpawnItemsOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // If starting with zero or fewer uses, this component is a no-op
        if (ent.Comp.Uses <= 0)
            return;

        var spawnEntities = GetSpawns(ent.Comp.Items, _random);

        var spawned = new List<EntityUid>();
        foreach (var proto in spawnEntities)
        {
            var spawn = PredictedSpawnNextToOrDrop(proto, args.User);
            spawned.Add(spawn);

            _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.User)} used {ToPrettyString(ent)} which spawned {ToPrettyString(spawn)}");
        }

        // The entity is often deleted, so play the sound at its position rather than parenting
        if (ent.Comp.Sound != null)
            _audio.PlayPredicted(ent.Comp.Sound, ent, args.User, ent.Comp.Sound.Params);

        ent.Comp.Uses--;

        // Delete entity only if component was successfully used
        var remove = false;
        if (ent.Comp.Uses <= 0)
        {
            _hands.TryDrop(args.User, ent);
            remove = true;
        }

        foreach (var spawn in spawned)
        {
            _hands.TryPickupAnyHand(args.User, spawn);
        }

        if (remove)
            PredictedQueueDel(ent);

        args.Handled = true;
    }
}
