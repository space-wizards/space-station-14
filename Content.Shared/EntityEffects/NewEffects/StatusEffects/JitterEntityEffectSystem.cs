using Content.Shared.Jittering;
using Content.Shared.StatusEffect;

namespace Content.Shared.EntityEffects.NewEffects;

// TODO: When Jittering is moved to new Status, make this use StatusEffectsContainerComponent.
/// <summary>
/// This is used for...
/// </summary>
public sealed partial class JitterEntityEffectSystem : EntityEffectSystem<StatusEffectsComponent, JitterEffect>
{
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    protected override void Effect(Entity<StatusEffectsComponent> entity, ref EntityEffectEvent<JitterEffect> args)
    {
        var time = args.Effect.Time * args.Scale;

        _jittering.DoJitter(entity, TimeSpan.FromSeconds(time), args.Effect.Refresh, args.Effect.Amplitude, args.Effect.Frequency);
    }
}

public sealed partial class JitterEffect : EntityEffectBase<JitterEffect>
{
    [DataField]
    public float Amplitude = 10.0f;

    [DataField]
    public float Frequency = 4.0f;

    [DataField]
    public float Time = 2.0f;

    /// <remarks>
    ///     true - refresh jitter time,  false - accumulate jitter time
    /// </remarks>
    [DataField]
    public bool Refresh = true;
}
