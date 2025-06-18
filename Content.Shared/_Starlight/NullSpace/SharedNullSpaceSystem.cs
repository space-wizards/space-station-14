using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Mobs;
using Robust.Shared.Physics.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;


namespace Content.Shared._Starlight.NullSpace;

public abstract class SharedEtherealSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    private EntProtoId ShadekinShadow = "ShadekinShadow";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NullSpaceComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<NullSpaceComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NullSpaceComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<NullSpaceComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<NullSpaceComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<NullSpaceComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<NullSpaceComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<NullSpaceComponent, PreventCollideEvent>(PreventCollision);
    }

    public virtual void OnStartup(EntityUid uid, NullSpaceComponent component, MapInitEvent args)
    {
    }

    public virtual void OnShutdown(EntityUid uid, NullSpaceComponent component, ComponentShutdown args)
    {
    }

    private void OnMobStateChanged(EntityUid uid, NullSpaceComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical
            || args.NewMobState == MobState.Dead)
        {
            SpawnAtPosition(ShadekinShadow, Transform(uid).Coordinates);
            RemComp(uid, component);
        }
    }

    private void OnShootAttempt(Entity<NullSpaceComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }

    private void OnAttackAttempt(EntityUid uid, NullSpaceComponent component, AttackAttemptEvent args)
    {
        if (HasComp<NullSpaceComponent>(args.Target))
            return;

        args.Cancel();
    }

    private void OnBeforeThrow(Entity<NullSpaceComponent> ent, ref BeforeThrowEvent args)
    {
        var thrownItem = args.ItemUid;

        // Raise an AttemptPacifiedThrow event and rely on other systems to check
        // whether the candidate item is OK to throw:
        var ev = new AttemptPacifiedThrowEvent(thrownItem, ent);
        RaiseLocalEvent(thrownItem, ref ev);
        if (!ev.Cancelled)
            return;

        args.Cancelled = true;
    }

    private void OnInteractionAttempt(EntityUid uid, NullSpaceComponent component, ref InteractionAttemptEvent args)
    {
        if (HasComp<NullSpaceComponent>(args.Target))
            return;

        args.Cancelled = true;
    }

    private void PreventCollision(EntityUid uid, NullSpaceComponent component, ref PreventCollideEvent args)
    {
        if (!_net.IsClient)
            args.Cancelled = true;
    }
}