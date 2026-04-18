using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Jittering;

/// <summary>
/// Handles "jitter" animations where a sprite moves around a point erratically.
/// </summary>
public abstract class SharedJitteringSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    // This prototype exists as a compatibility layer with previous jittering.
    // Ideally nothing calls `CreateJitter` but instead goes through status effects in their own way
    private static readonly EntProtoId BasicJitter = "StatusEffectJitter";

    public void AdjustJitter(EntityUid target,
                            EntProtoId<JitteringStatusEffectComponent> statusId,
                            JitterParameters jitter)
    {
        if (!_statusEffects.TryGetStatusEffect(target, statusId, out var statusEnt))
            return;

        var jitterComp = EnsureComp<JitteringStatusEffectComponent>(statusEnt.Value);
        jitterComp.Jitter = jitter;
        Dirty(statusEnt.Value, jitterComp);
    }

    /// <summary>
    /// Creates a new status effect on an entity that causes its sprite to move erratically.
    /// </summary>
    /// <param name="target">The entity that will begin jittering.</param>
    /// <param name="jitter">What kind of jitter to apply.</param>
    /// <param name="duration">How long the entity should jitter. Permanent if null.</param>
    /// <param name="refresh">Status duration is set if true, or accumulated if false.</param>
    [Obsolete("Jittering should be applied through StatusEffectsSystem and not directly.")]
    public void CreateJitter(EntityUid target, JitterParameters? jitter = null, TimeSpan? duration = null, bool refresh = false)
    {
        EntityUid? statusEnt;
        if (!refresh && duration != null)
        {
            if (!_statusEffects.TryAddStatusEffectDuration(target, BasicJitter, out statusEnt, duration.Value))
                return;
        }
        else
        {
            if (!_statusEffects.TryUpdateStatusEffectDuration(target, BasicJitter, out statusEnt, duration))
                return;
        }

        var jitterComp = EnsureComp<JitteringStatusEffectComponent>(statusEnt.Value);
        if (jitter == null)
            return;

        jitterComp.Jitter = jitter.Value;
        Dirty(statusEnt.Value, jitterComp);
    }

    /// <summary>
    /// Removes jitter effects applied by <see cref="CreateJitter"/>.
    /// Important if a duration was not set.
    /// </summary>
    [Obsolete("Lifestage of jittering should be handled by StatusEffectsSystem.")]
    public void RemoveJitter(EntityUid target)
    {
        _statusEffects.TryRemoveStatusEffect(target, BasicJitter);
    }
}
