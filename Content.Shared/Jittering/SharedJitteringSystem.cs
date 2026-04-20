using Content.Shared.StatusEffectNew;
using JetBrains.Annotations;
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

    /// <summary>
    /// Adjusts the jittering of an active status effect.
    /// Adds jittering if the status did not previously have it.
    /// </summary>
    /// <param name="statusEnt">An active status effect.</param>
    /// <param name="jitter">The new parameters for the jitter.</param>
    [PublicAPI]
    public void AdjustJitter(EntityUid statusEnt, JitterParameters jitter)
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
