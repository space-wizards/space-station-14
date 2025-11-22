using Content.Shared._Offbrand.Weapons;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class MeleeModifiersStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeModifiersStatusEffectComponent, StatusEffectRelayedEvent<RelayedGetMeleeDamageEvent>>(OnGetMeleeDamage);
        SubscribeLocalEvent<MeleeModifiersStatusEffectComponent, StatusEffectRelayedEvent<RelayedGetMeleeAttackRateEvent>>(OnGetMeleeAttackRate);
    }

    private void OnGetMeleeDamage(Entity<MeleeModifiersStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RelayedGetMeleeDamageEvent> args)
    {
        if (ent.Comp.DamageModifier is { } modifier)
            args.Args.Args.Modifiers.Add(modifier);
    }

    private void OnGetMeleeAttackRate(Entity<MeleeModifiersStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RelayedGetMeleeAttackRateEvent> args)
    {
        args.Args = args.Args with { Args = args.Args.Args with { Multipliers = args.Args.Args.Multipliers * ent.Comp.AttackRateMultiplier, Rate = args.Args.Args.Rate + ent.Comp.AttackRateConstant } };
    }
}
