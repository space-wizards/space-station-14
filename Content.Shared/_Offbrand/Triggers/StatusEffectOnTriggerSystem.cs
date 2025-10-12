using Content.Shared.StatusEffectNew;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger;

namespace Content.Shared._Offbrand.Triggers;

public sealed class StatusEffectOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddStatusEffectOnTriggerComponent, TriggerEvent>(OnTriggerAdd);
        SubscribeLocalEvent<UpdateStatusEffectOnTriggerComponent, TriggerEvent>(OnTriggerUpdate);
        SubscribeLocalEvent<SetStatusEffectOnTriggerComponent, TriggerEvent>(OnTriggerSet);
        SubscribeLocalEvent<RemoveStatusEffectOnTriggerComponent, TriggerEvent>(OnTriggerRemove);
    }

    private void OnTriggerAdd(Entity<AddStatusEffectOnTriggerComponent> trigger, ref TriggerEvent args)
    {
        if ((trigger.Comp.TargetUser ? args.User : trigger.Owner) is not { } target)
            return;

        if (args.Key != null && !trigger.Comp.KeysIn.Contains(args.Key))
            return;

        _statusEffects.TryAddStatusEffectDuration(target, trigger.Comp.EffectProto, trigger.Comp.Duration);
    }

    private void OnTriggerUpdate(Entity<UpdateStatusEffectOnTriggerComponent> trigger, ref TriggerEvent args)
    {
        if ((trigger.Comp.TargetUser ? args.User : trigger.Owner) is not { } target)
            return;

        if (args.Key != null && !trigger.Comp.KeysIn.Contains(args.Key))
            return;

        _statusEffects.TryUpdateStatusEffectDuration(target, trigger.Comp.EffectProto, trigger.Comp.Duration);
    }

    private void OnTriggerSet(Entity<SetStatusEffectOnTriggerComponent> trigger, ref TriggerEvent args)
    {
        if ((trigger.Comp.TargetUser ? args.User : trigger.Owner) is not { } target)
            return;

        if (args.Key != null && !trigger.Comp.KeysIn.Contains(args.Key))
            return;

        _statusEffects.TrySetStatusEffectDuration(target, trigger.Comp.EffectProto, trigger.Comp.Duration);
    }

    private void OnTriggerRemove(Entity<RemoveStatusEffectOnTriggerComponent> trigger, ref TriggerEvent args)
    {
        if ((trigger.Comp.TargetUser ? args.User : trigger.Owner) is not { } target)
            return;

        if (args.Key != null && !trigger.Comp.KeysIn.Contains(args.Key))
            return;

        if (trigger.Comp.Duration is { } duration)
            _statusEffects.TryAddTime(target, trigger.Comp.EffectProto, -duration);
        else
            _statusEffects.TryRemoveStatusEffect(target, trigger.Comp.EffectProto);
    }
}
