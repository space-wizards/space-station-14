using JetBrains.Annotations;

namespace Content.Shared.Jittering;

/// <summary>
/// Handles "jitter" animations where a sprite erratically moves around its origin.
/// </summary>
public abstract partial class SharedJitteringSystem : EntitySystem
{
    /// <summary>
    /// Adjusts the jittering of an active status effect.
    /// Adds jittering if the status did not previously have it.
    /// </summary>
    /// <param name="statusEnt">An active status effect.</param>
    /// <param name="jitter">The new parameters for the jitter.</param>
    [PublicAPI]
    public virtual void AdjustJitter(EntityUid statusEnt, JitterParameters jitter)
    {
        var jitterComp = EnsureComp<JitteringStatusEffectComponent>(statusEnt);
        jitterComp.Jitter = jitter;
        Dirty(statusEnt, jitterComp);
    }

    /// <summary>
    /// Removes the jittering of a status effect.
    /// </summary>
    /// <param name="statusEnt">An active status effect.</param>
    [PublicAPI]
    public void RemoveJitter(EntityUid statusEnt)
    {
        RemCompDeferred<JitteringStatusEffectComponent>(statusEnt);
    }
}
