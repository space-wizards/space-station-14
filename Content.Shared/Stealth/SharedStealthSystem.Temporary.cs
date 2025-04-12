using Content.Shared.Stealth.Components;

namespace Content.Shared.Stealth;

public abstract partial class SharedStealthSystem
{
    private void InitializeTemporary()
    {
        SubscribeLocalEvent<TemporaryStealthComponent, MapInitEvent>(OnTempVisibilityStart);
        SubscribeLocalEvent<TemporaryStealthComponent, GetVisibilityModifiersEvent>(OnGetTempVisibilityModifiers);
    }

    private void UpdateTemporary(float frameTime)
    {
        var query = EntityQueryEnumerator<TemporaryStealthComponent>();
        while (query.MoveNext(out var uid, out var temp))
        {
            if (_timing.CurTime - temp.StartTime > temp.FadeInTime + temp.Duration + temp.FadeOutTime)
            {
                if (temp.RemoveStealth)
                    RemCompDeferred<StealthComponent>(uid);

                RemCompDeferred<TemporaryStealthComponent>(uid);
            }
        }
    }

    private void OnTempVisibilityStart(Entity<TemporaryStealthComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.RemoveStealth = !EnsureComp<StealthComponent>(ent, out var stealth);
        ent.Comp.StartTime = _timing.CurTime;
    }

    public void AddTemporaryStealth(EntityUid uid, TimeSpan time)
    {
        var temp = EnsureComp<TemporaryStealthComponent>(uid);

        temp.Duration += time;

        Dirty(uid, temp);
    }

    private void OnGetTempVisibilityModifiers(Entity<TemporaryStealthComponent> ent, ref GetVisibilityModifiersEvent args)
    {
        var currentTime = _timing.CurTime;
        var elapsed = currentTime - ent.Comp.StartTime;

        // Phase 1 - Fade in
        if (elapsed < ent.Comp.FadeInTime)
        {
            var progress = (float)(elapsed / ent.Comp.FadeInTime);
            args.FlatModifier += ent.Comp.TargetVisibility * progress;
        }
        // Phase 2 - Main duration
        else if (elapsed < ent.Comp.FadeInTime + ent.Comp.Duration)
        {
            args.FlatModifier += ent.Comp.TargetVisibility;
        }
        // Phase 3 - Fade out
        else if (elapsed < ent.Comp.FadeInTime + ent.Comp.Duration + ent.Comp.FadeOutTime)
        {
            var progress = (float)((elapsed - ent.Comp.FadeInTime - ent.Comp.Duration) / ent.Comp.FadeOutTime);
            args.FlatModifier += ent.Comp.TargetVisibility * (1f - progress);
        }
    }
}
