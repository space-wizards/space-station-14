using Content.Shared.Jittering;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Applies the Jittering Status Effect to this entity.
/// The amount of time the Jittering is applied is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class JitterEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Jitter>
{
    [Dependency] private SharedJitteringSystem _jittering = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Jitter> args)
    {
        var time = args.Effect.Time * args.Scale;

        _jittering.DoJitter(entity, TimeSpan.FromSeconds(time), args.Effect.Refresh, args.Effect.Amplitude, args.Effect.Frequency);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Jitter : EntityEffectBase<Jitter>
{
    [DataField]
    public float Amplitude = 10.0f;

    [DataField]
    public float Frequency = 4.0f;

    [DataField]
    public float Time = 2.0f;

    /// <remarks>
    /// true - refresh jitter time, false - accumulate jitter time
    /// </remarks>
    [DataField]
    public bool Refresh = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-jittering", ("chance", Probability));
}
