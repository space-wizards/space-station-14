using Content.Shared.Jittering;
using Content.Shared.StatusEffect;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

// TODO: When Jittering is moved to new Status, make this use StatusEffectsContainerComponent.
/// <summary>
/// This is used for...
/// </summary>
public sealed partial class JitterEntityEffectSystem : EntityEffectSystem<StatusEffectsComponent, Jitter>
{
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    protected override void Effect(Entity<StatusEffectsComponent> entity, ref EntityEffectEvent<Jitter> args)
    {
        var time = args.Effect.Time * args.Scale;

        _jittering.DoJitter(entity, TimeSpan.FromSeconds(time), args.Effect.Refresh, args.Effect.Amplitude, args.Effect.Frequency);
    }
}

[DataDefinition]
public sealed partial class Jitter : EntityEffectBase<Jitter>
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
