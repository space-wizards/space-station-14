using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stealth.Components;

namespace Content.Shared.Stealth;

public abstract partial class SharedStealthSystem
{
    private void InitializeStatusEffect()
    {
        SubscribeLocalEvent<StealthStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<StealthStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
        SubscribeLocalEvent<StealthStatusEffectComponent, StatusEffectRelayedEvent<GetVisibilityModifiersEvent>>(OnGetTempVisibilityModifiers);
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
        ent.Comp.RemoveStealth = !EnsureComp<StealthComponent>(args.Target, out _);
    }

    private void OnGetTempVisibilityModifiers(Entity<StealthStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetVisibilityModifiersEvent> args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var statusEffectComp))
            return;

        var currentTime = _timing.CurTime;
        var elapsed = currentTime - statusEffectComp.StartEffectTime;
        var duration = statusEffectComp.EndEffectTime - statusEffectComp.StartEffectTime;

        // Phase 1 - Fade in
        if (elapsed < ent.Comp.FadeInTime)
        {
            var progress = (float)(elapsed / ent.Comp.FadeInTime);
            args.Args.FlatModifier += ent.Comp.TargetVisibility * progress;
        }
        // Phase 2 - Main duration
        else if (elapsed < ent.Comp.FadeInTime + duration)
        {
            args.Args.FlatModifier += ent.Comp.TargetVisibility;
        }
        // Phase 3 - Fade out
        else if (duration is not null && elapsed < ent.Comp.FadeInTime + duration + ent.Comp.FadeOutTime)
        {
            var progress = (float)((elapsed - ent.Comp.FadeInTime - duration) / ent.Comp.FadeOutTime);
            args.Args.FlatModifier += ent.Comp.TargetVisibility * (1f - progress);
        }
    }
}
