using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.CosmicCult.Abilities;

public sealed partial class CosmicLapseSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private CosmicCultSystem _cult = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    [SubscribeLocalEvent]
    private void OnLapseAbility(Entity<CosmicActionLapseComponent> ent, ref EventCosmicLapse args)
    {
        if (!_cult.CultActionQuery.TryComp(ent, out var action) || args.Handled)
            return;

        var species = Comp<HumanoidProfileComponent>(args.Target).Species;
        var lapseEnt = PredictedSpawnAtPosition(ent.Comp.SpawnLapse, Transform(args.Target).Coordinates);

        args.Handled = true;
        var doAfterArgs = new DoAfterArgs(EntityManager,
            lapseEnt,
            ent.Comp.Duration,
            new CosmicLapseDoAfter(),
            lapseEnt,
            args.Target)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            BreakOnMove = false,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
        };

        if (_net.IsServer && _container.TryGetContainer(lapseEnt, ent.Comp.Container, out var container))
        {
            _container.Insert(args.Target, container, force: true);
            SpawnAttachedTo(action.Vfx, Transform(lapseEnt).Coordinates);
        }

        EnsureComp<CosmicVisualFadeComponent>(lapseEnt, out var fade);
        fade.Duration = (float) ent.Comp.Duration.TotalSeconds;
        Dirty(lapseEnt, fade);

        _doAfter.TryStartDoAfter(doAfterArgs);
        _appearance.SetData(lapseEnt, CosmicLapseVisuals.Visuals, species);
        _audio.PlayPredicted(action.Sfx, args.Performer, args.Performer, AudioParams.Default.WithVariation(0.075f));
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<CosmicLapsedComponent> ent, ref CosmicLapseDoAfter args)
    {
        _audio.PlayPredicted(ent.Comp.Sfx, ent, ent, AudioParams.Default.WithVariation(0.075f));

        if (_net.IsServer )
            SpawnAttachedTo(ent.Comp.Vfx, Transform(ent).Coordinates);

        if (_container.TryGetContainer(ent, SharedEntityStorageSystem.ContainerName, out var container))
            _container.EmptyContainer(container, true);

        QueueDel(ent);
    }
}

// }
//
//     {
//         if (!_cult.CultActionQuery.TryComp(ent, out var action) || args.Handled)
//             return;
//
//         if (StuffIntoStorage(args.Target, ent.Comp.SpawnLapse, ent.Comp.Container) is not { } lapseEnt)
//             return;
//
//         args.Handled = true;
//
//         var species = Comp<HumanoidProfileComponent>(args.Target).Species;
//         var doAfterArgs = new DoAfterArgs(EntityManager, lapseEnt, ent.Comp.Duration, new CosmicLapseDoAfter(), lapseEnt, args.Target)
//         {
//             NeedHand = false,
//             BreakOnWeightlessMove = false,
//             BreakOnMove = false,
//             BreakOnHandChange = false,
//             BreakOnDropItem = false,
//             BreakOnDamage = false,
//             RequireCanInteract = false,
//         };
//
//         if (_net.IsClient && _timing.IsFirstTimePredicted)
//             SpawnAttachedTo(action.Vfx, Transform(lapseEnt).Coordinates);
//
//         _doAfter.TryStartDoAfter(doAfterArgs);
//         _appearance.SetData(lapseEnt, CosmicLapseVisuals.Visuals, species);
//         _audio.PlayPredicted(action.Sfx, args.Performer, args.Performer, AudioParams.Default.WithVariation(0.075f));
//     }
//
//     private EntityUid? StuffIntoStorage(EntityUid target, EntProtoId toSpawn, string container)
//     {
//         if (!_net.IsServer)
//             return null;
//
//         var containerEntity = SpawnAtPosition(toSpawn, Transform(target).Coordinates);
//
//         if (!_container.TryGetContainer(containerEntity, container, out var storage))
//         {
//             QueueDel(containerEntity);
//             return null;
//         }
//
//         if (!_container.Insert(target, storage, force: true))
//             QueueDel(containerEntity);
//
//         return containerEntity;
//     }
//
//     [SubscribeLocalEvent]
//     private void OnDoAfter(Entity<CosmicLapsedComponent> ent, ref CosmicLapseDoAfter args)
//     {
//         _audio.PlayPredicted(ent.Comp.Sfx, ent, ent, AudioParams.Default.WithVariation(0.075f));
//
//         if (_net.IsClient && _timing.IsFirstTimePredicted)
//             SpawnAttachedTo(ent.Comp.Vfx, Transform(ent).Coordinates);
//
//         if (_container.TryGetContainer(ent, SharedEntityStorageSystem.ContainerName, out var container))
//             _container.EmptyContainer(container, true);
//
//         QueueDel(ent);
//     }
// }
