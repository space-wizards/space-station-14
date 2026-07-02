using Content.Shared.Jittering;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

// TODO: When Jittering is moved to new Status, make this use StatusEffectsContainerComponent.
/// <summary>
/// Applies the Jittering Status Effect to this entity.
/// The amount of time the Jittering is applied is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class JitterEntityEffectSystem : EntityEffectSystem<StatusEffectsComponent, Jitter>
{
    [Dependency] private SharedJitteringSystem _jittering = default!;

    protected override void Effect(Entity<StatusEffectsComponent> entity, Jitter effect, EntityEffectData data)
    {
        var time = effect.Time * data.Scale;

        _jittering.DoJitter(entity, TimeSpan.FromSeconds(time), effect.Refresh, effect.Amplitude, effect.Frequency);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Jitter : EntityEffect
{
    /// <summary>
    /// How intense the jitter is.
    /// </summary>
    [DataField]
    public float Amplitude = 10.0f;

    /// <summary>
    /// The speed of the jitter.
    /// </summary>
    [DataField]
    public float Frequency = 4.0f;

    /// <summary>
    /// The duration of the effect.
    /// </summary>
    [DataField]
    public float Time = 2.0f;

    /// <summary>
    /// True - refresh jitter time,  false - accumulate jitter time.
    /// </summary>
    [DataField]
    public bool Refresh = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-jittering", ("chance", Probability));
}
