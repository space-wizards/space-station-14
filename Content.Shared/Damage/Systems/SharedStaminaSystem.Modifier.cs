using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Damage.Systems;

public partial class SharedStaminaSystem
{
    private void InitializeModifier()
    {
        SubscribeLocalEvent<StaminaModifierStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<StaminaModifierStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
        SubscribeLocalEvent<StaminaModifierStatusEffectComponent, StatusEffectRelayedEvent<RefreshStaminaCritThresholdEvent>>(OnRefreshCritThreshold);
    }

    private void OnEffectApplied(Entity<StaminaModifierStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        RefreshStaminaCritThreshold(args.Target);
    }

    private void OnEffectRemoved(Entity<StaminaModifierStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RefreshStaminaCritThreshold(args.Target);
    }

    private void OnRefreshCritThreshold(Entity<StaminaModifierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshStaminaCritThresholdEvent> args)
    {
        var evArgs = args.Args;
        evArgs.Modifier = Math.Max(ent.Comp.Modifier, evArgs.Modifier); // We only pick the highest value, to avoid stacking different status effects.
        args.Args = evArgs;
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
