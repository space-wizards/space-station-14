using System.Collections.Generic;
using Content.Server.DoAfter;
using Content.Server.Mining.Components;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Acts;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Mining.Systems;

public class AsteroidRockSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActSystem _actSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AsteroidRockComponent, DestructionEventArgs>(OnAsteroidRockDestruction);
        SubscribeLocalEvent<AsteroidRockComponent, InteractUsingEvent>(OnAsteroidRockInteractUsing);
        SubscribeLocalEvent<AsteroidRockComponent, MineDoAfterComplete>(RockMined);
    }

    private void RockMined(EntityUid uid, AsteroidRockComponent component, MineDoAfterComplete args)
    {
        _actSystem.HandleDestruction(uid);
    }

    private void OnAsteroidRockDestruction(EntityUid uid, AsteroidRockComponent component, DestructionEventArgs args)
    {
        if (!_random.Prob(component.OreChance))
            return; // Nothing to do.

        HashSet<string> spawnedGroups = new();
        foreach (var entry in component.OreTable)
        {
            if (entry.GroupId is not null && spawnedGroups.Contains(entry.GroupId))
                continue;

            if (!_random.Prob(entry.SpawnProbability))
                continue;

            for (var i = 0; i < entry.Amount; i++)
            {
                Spawn(entry.PrototypeId, Transform(uid).Coordinates);
            }

            if (entry.GroupId != null)
                spawnedGroups.Add(entry.GroupId);
        }
    }

    private void OnAsteroidRockInteractUsing(EntityUid uid, AsteroidRockComponent component, InteractUsingEvent args)
    {
        if (!TryComp<PickaxeComponent>(args.Used, out var pickaxeComponent))
        {
            return;
        }

        _doAfter.DoAfter(new DoAfterEventArgs
        (
            args.User,
            0.8f,
            default,
            uid
        )
        {
            TargetFinishedEvent = new MineDoAfterComplete()
        });

        SoundSystem.Play(Filter.Pvs(uid), pickaxeComponent.MiningSound.GetSound(), uid, AudioParams.Default);

        args.Handled = true;
    }

    private class MineDoAfterComplete : EntityEventArgs { }
}
