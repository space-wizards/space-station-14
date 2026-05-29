using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Jittering;

/// <summary>
/// A system for applying a jitter animation to any entity.
/// </summary>
public abstract partial class SharedJitteringSystem : EntitySystem
{
    public static readonly EntProtoId JitterStatusEffect = "StatusEffectJitter";

    [Dependency] protected IGameTiming GameTiming = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public float MaxAmplitude = 300f;
    public float MinAmplitude = 1f;

    public float MaxFrequency = 10f;
    public float MinFrequency = 1f;

    public override void Initialize()
    {
        SubscribeLocalEvent<JitteringComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectAppliedEvent>(OnJitteringStatusApplied);
        SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectRemovedEvent>(OnJitteringStatusRemoved);
    }

    private void OnRejuvenate(EntityUid uid, JitteringComponent component, RejuvenateEvent args)
    {
        RemCompDeferred<JitteringComponent>(uid);
    }

    private void OnJitteringStatusApplied(Entity<JitteringStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        ApplyJitteringFromStatusEffect(args.Target, entity.Comp);
    }

    private void OnJitteringStatusRemoved(Entity<JitteringStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        RemComp<JitteringComponent>(args.Target);
    }

    /// <summary>
    /// Applies a jitter effect to the specified entity.
    /// You can apply this to any entity whatsoever, so be careful what you use it on!
    /// </summary>
    /// <remarks>
    /// If the entity is already jittering, the jitter values will be updated but only if they're greater
    /// than the current ones and <see cref="forceValueChange"/> is false.
    /// </remarks>
    public void DoJitter(
        EntityUid uid,
        TimeSpan time,
        bool refresh,
        float amplitude = 10f,
        float frequency = 4f,
        bool forceValueChange = false)
    {
        if (time <= TimeSpan.Zero)
            return;

        if (refresh)
        {
            if (!_status.TrySetStatusEffectDuration(uid, JitterStatusEffect, time))
                return;
        }
        else if (!_status.TryAddStatusEffectDuration(uid, JitterStatusEffect, time))
        {
            return;
        }

        if (!_status.TryGetStatusEffect(uid, JitterStatusEffect, out var effect))
            return;

        var statusComp = EnsureComp<JitteringStatusEffectComponent>(effect.Value);
        statusComp.Amplitude = Math.Clamp(amplitude, MinAmplitude, MaxAmplitude);
        statusComp.Frequency = Math.Clamp(frequency, MinFrequency, MaxFrequency);
        statusComp.ForceValueChange = forceValueChange;
        Dirty(effect.Value, statusComp);

        ApplyJitteringFromStatusEffect(uid, statusComp);
    }

    /// <summary>
    /// For non mobs.
    /// </summary>
    public void AddJitter(EntityUid uid, float amplitude = 10f, float frequency = 4f)
    {
        var jitter = EnsureComp<JitteringComponent>(uid);
        jitter.Amplitude = amplitude;
        jitter.Frequency = frequency;
        Dirty(uid, jitter);
    }

    private void ApplyJitteringFromStatusEffect(EntityUid target, JitteringStatusEffectComponent comp)
    {
        var amplitude = Math.Clamp(comp.Amplitude, MinAmplitude, MaxAmplitude);
        var frequency = Math.Clamp(comp.Frequency, MinFrequency, MaxFrequency);
        ApplyJitteringValues(target, amplitude, frequency, comp.ForceValueChange);
    }

    private void ApplyJitteringValues(EntityUid target, float amplitude, float frequency, bool forceValueChange)
    {
        var jittering = EnsureComp<JitteringComponent>(target);

        if (forceValueChange || jittering.Amplitude < amplitude)
            jittering.Amplitude = amplitude;

        if (forceValueChange || jittering.Frequency < frequency)
            jittering.Frequency = frequency;

        Dirty(target, jittering);
    }
}
