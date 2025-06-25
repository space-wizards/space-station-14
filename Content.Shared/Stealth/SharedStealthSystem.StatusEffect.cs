using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stealth.Components;

namespace Content.Shared.Stealth;

public abstract partial class SharedStealthSystem
{
    [Dependency] private readonly SharedStatusEffectsSystem _statusEffects = default!;

    private void InitializeStatusEffect()
    {
        SubscribeLocalEvent<StealthStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<StealthStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
        SubscribeLocalEvent<StatusEffectContainerComponent, GetVisibilityModifiersEvent>(OnGetTempVisibilityModifiers); //TODO: We 100% need some status effect events relay!
    }

    private void OnEffectRemoved(Entity<StealthStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        //TODO: Well, I suspect that the logic of using multiple status effects applied one at a time would be completely broken here.
        //However, this seems to me to be a problem with the underlying StealthSystem architecture rather than a status effect problem

        //However, as long as we only have 1 status effect type imposing invisibility - everything should work fine.

        if (ent.Comp.RemoveStealth)
            RemCompDeferred<StealthComponent>(args.Target);
    }

    private void OnEffectApplied(Entity<StealthStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.RemoveStealth = !EnsureComp<StealthComponent>(args.Target, out var stealth);
    }

    private void OnGetTempVisibilityModifiers(Entity<StatusEffectContainerComponent> ent, ref GetVisibilityModifiersEvent args)
    {
        if (!_statusEffects.TryEffectsWithComp<StealthStatusEffectComponent>(ent, out var stealthEffects))
            return;

        foreach (var effect in stealthEffects)
        {
            var currentTime = _timing.CurTime;
            var elapsed = currentTime - effect.Comp2.StartEffectTime;
            var duration = effect.Comp2.EndEffectTime - effect.Comp2.StartEffectTime;

            // Phase 1 - Fade in
            if (elapsed < effect.Comp1.FadeInTime)
            {
                var progress = (float)(elapsed / effect.Comp1.FadeInTime);
                args.FlatModifier += effect.Comp1.TargetVisibility * progress;
            }
            // Phase 2 - Main duration
            else if (elapsed < effect.Comp1.FadeInTime + duration)
            {
                args.FlatModifier += effect.Comp1.TargetVisibility;
            }
            // Phase 3 - Fade out
            else if (duration is not null && elapsed < effect.Comp1.FadeInTime + duration + effect.Comp1.FadeOutTime)
            {
                var progress = (float)((elapsed - effect.Comp1.FadeInTime - duration) / effect.Comp1.FadeOutTime);
                args.FlatModifier += effect.Comp1.TargetVisibility * (1f - progress);
            }
        }
    }
}
