using Content.Shared.StatusEffectNew;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class SetStatusEffectOnTriggerSystem : XOnTriggerSystem<SetStatusEffectOnTriggerComponent>
{
    [Dependency] private StatusEffectsSystem _status = default!;

    protected override void OnTrigger(Entity<SetStatusEffectOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        args.Handled |= _status.TrySetStatusEffectDuration(target, ent.Comp.Status, ent.Comp.Duration);
    }
}

public sealed partial class RemoveStatusEffectOnTriggerSystem : XOnTriggerSystem<RemoveStatusEffectOnTriggerComponent>
{
    [Dependency] private StatusEffectsSystem _status = default!;

    protected override void OnTrigger(Entity<RemoveStatusEffectOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        args.Handled |=  _status.TryRemoveStatusEffect(target, ent.Comp.Status);
    }
}
