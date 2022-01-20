using System.Collections.Generic;
using Content.Server.DoAfter;
using Content.Server.Mining.Components;
using Content.Shared.Acts;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Mining;

public class MineableSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MineableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MiningDoafterCancel>(OnDoafterCancel);
        SubscribeLocalEvent<MiningDoafterSuccess>(OnDoafterSuccess);
        SubscribeLocalEvent<MineableComponent, DestructionEventArgs>(OnAsteroidRockDestruction);
    }

    private void OnInteractUsing(EntityUid uid, MineableComponent component, InteractUsingEvent args)
    {
        if (!TryComp<PickaxeComponent>(args.Used, out var pickaxe))
            return;

        // Can't mine too many entities at once.
        if (pickaxe.MaxMiningEntities < pickaxe.MiningEntities.Count + 1)
            return;

        // Can't mine one object multiple times.
        if (!pickaxe.MiningEntities.Add(uid))
            return;

        var doAfter = new DoAfterEventArgs(args.User, component.BaseMineTime * pickaxe.MiningTimeMultiplier, default, uid)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            BroadcastCancelledEvent = new MiningDoafterCancel() { Pickaxe = args.Used, Rock = uid },
            BroadcastFinishedEvent = new MiningDoafterSuccess() { Pickaxe = args.Used, Rock = uid }
        };

        _doAfterSystem.DoAfter(doAfter);
    }

    private void OnDoafterSuccess(MiningDoafterSuccess ev)
    {
        if (!TryComp(ev.Pickaxe, out PickaxeComponent? pickaxe))
            return;

        _damageableSystem.TryChangeDamage(ev.Rock, pickaxe.Damage);
        SoundSystem.Play(Filter.Pvs(ev.Rock), pickaxe.MiningSound.GetSound(), AudioParams.Default);
        pickaxe.MiningEntities.Remove(ev.Rock);
    }

    private void OnDoafterCancel(MiningDoafterCancel ev)
    {
        if (!TryComp(ev.Pickaxe, out PickaxeComponent? pickaxe))
            return;

        pickaxe.MiningEntities.Remove(ev.Rock);
    }

    private void OnAsteroidRockDestruction(EntityUid uid, MineableComponent component, DestructionEventArgs args)
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
}

// grumble grumble
public class MiningDoafterSuccess : EntityEventArgs
{
    public EntityUid Pickaxe;
    public EntityUid Rock;
}

public class MiningDoafterCancel : EntityEventArgs
{
    public EntityUid Pickaxe;
    public EntityUid Rock;
}
