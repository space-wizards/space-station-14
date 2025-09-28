using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class DisruptOnAttackStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisruptOnAttackEvent>(OnDisruptOnAttack);
        SubscribeLocalEvent<DisruptOnAttackStatusEffectComponent, StatusEffectRelayedEvent<DamageChangedEvent>>(OnDamageChanged);
    }

    private void OnDisruptOnAttack(DisruptOnAttackEvent args)
    {
        var disarm = new DisarmedEvent(args.Damaged, args.Origin, 1f);
        RaiseLocalEvent(args.Damaged, ref disarm);

        _userInterface.CloseUserUis(args.Damaged);
    }

    private void OnDamageChanged(Entity<DisruptOnAttackStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageChangedEvent> args)
    {
        if (!args.Args.DamageIncreased)
            return;

        if (args.Args.Origin is not {} origin)
            return;

        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } damaged)
            return;

        if (!HasComp<MobStateComponent>(origin))
            return;

        QueueLocalEvent(new DisruptOnAttackEvent(damaged, origin));
    }
}

public sealed class DisruptOnAttackEvent(EntityUid damaged, EntityUid origin) : EntityEventArgs
{
    public readonly EntityUid Damaged = damaged;
    public readonly EntityUid Origin = origin;
}

