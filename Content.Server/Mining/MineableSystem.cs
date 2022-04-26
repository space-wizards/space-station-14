using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Mining.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Mining;

public sealed class MineableSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MineableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MiningDoafterCancel>(OnDoafterCancel);
        SubscribeLocalEvent<MineableComponent, MiningDoafterSuccess>(OnDoafterSuccess);
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

        pickaxe.CancelToken = new CancellationTokenSource();
        var doAfter = new DoAfterEventArgs(args.User, component.BaseMineTime * pickaxe.MiningTimeMultiplier, pickaxe.CancelToken.Token, uid)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            BroadcastCancelledEvent = new MiningDoafterCancel(component) { Pickaxe = args.Used, Rock = uid },
            TargetFinishedEvent = new MiningDoafterSuccess() { Pickaxe = args.Used, Rock = uid, Player = args.User }
        };

        _doAfterSystem.DoAfter(doAfter);
    }

    private void OnDoafterSuccess(EntityUid uid, MineableComponent component, MiningDoafterSuccess ev)
    {
        if (!TryComp(ev.Pickaxe, out PickaxeComponent? pickaxe))
            return;

        _damageableSystem.TryChangeDamage(ev.Rock, pickaxe.Damage);
        SoundSystem.Play(Filter.Pvs(ev.Rock), pickaxe.MiningSound.GetSound(), ev.Rock, AudioParams.Default);
        pickaxe.MiningEntities.Remove(ev.Rock);
        
        var spawnOre = EntitySpawnCollection.GetSpawns(component.Ores, _random);
        EntityManager.SpawnEntity(spawnOre[0], EntityManager.GetComponent<TransformComponent>(ev.Player).Coordinates);
        pickaxe.CancelToken = null;
    }

    private void OnDoafterCancel(MiningDoafterCancel ev)
    {
        if (!TryComp<PickaxeComponent>(ev.Pickaxe, out var pickaxe))
            return;
        
        pickaxe.CancelToken = null;
        pickaxe.MiningEntities.Remove(ev.Rock);
    }
    
    private sealed class MiningDoafterCancel : EntityEventArgs
    {
        public MineableComponent Component;
        public EntityUid Pickaxe;
        public EntityUid Rock;
        public MiningDoafterCancel(MineableComponent component)
        {
            Component = component;
        }
        
    }
}

// grumble grumble
public sealed class MiningDoafterSuccess : EntityEventArgs
{
    public EntityUid Pickaxe;
    public EntityUid Rock;
    public EntityUid Player;
}

