using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class SlowOnDamageModifierStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlowOnDamageModifierStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<SlowOnDamageModifierStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<SlowOnDamageModifierStatusEffectComponent, StatusEffectRelayedEvent<ModifySlowOnDamageSpeedEvent>>(OnModifySlowOnDamageSpeed);
    }

    private void OnStatusEffectApplied(Entity<SlowOnDamageModifierStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<SlowOnDamageModifierStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnModifySlowOnDamageSpeed(Entity<SlowOnDamageModifierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ModifySlowOnDamageSpeedEvent> args)
    {
        var delta = 1f - args.Args.Speed;
        if (delta <= 0f)
            return;

        args.Args = args.Args with { Speed = args.Args.Speed + delta * ent.Comp.Modifier };
    }
}
