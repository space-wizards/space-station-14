using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Damage.Systems;

public partial class SharedStaminaSystem
{
    [SubscribeLocalEvent]
    private void OnEffectApplied(Entity<StaminaModifierStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        RefreshStaminaCritThreshold(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnEffectRemoved(Entity<StaminaModifierStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RefreshStaminaCritThreshold(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRefreshCritThreshold(Entity<StaminaModifierStatusEffectComponent> ent, ref RefreshStaminaCritThresholdEvent args)
    {
        args.Modifier = Math.Max(ent.Comp.Modifier, args.Modifier); // We only pick the highest value, to avoid stacking different status effects.
    }

    public void RefreshStaminaCritThreshold(Entity<StaminaComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        var ev = new RefreshStaminaCritThresholdEvent(entity.Comp.BaseCritThreshold);
        RaiseLocalEvent(entity, ref ev);

        entity.Comp.CritThreshold = ev.ThresholdValue * ev.Modifier;
    }
}
