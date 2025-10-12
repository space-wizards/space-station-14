using Content.Shared.StatusEffectNew;
using Content.Shared.Trigger;

namespace Content.Shared._Offbrand.Triggers;

public sealed class StatusEffectTriggerConditionSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<StatusEffectTriggerConditionComponent> trigger, ref AttemptTriggerEvent args)
    {
        if ((trigger.Comp.TargetUser ? args.User : trigger.Owner) is not { } target)
            return;

        if (args.Key == null || trigger.Comp.Keys.Contains(args.Key))
            args.Cancelled |= !(_statusEffects.HasStatusEffect(target, trigger.Comp.EffectProto) ^ trigger.Comp.Invert);
    }
}
