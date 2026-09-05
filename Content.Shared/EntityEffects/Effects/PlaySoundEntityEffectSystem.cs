using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Causes this entity to glow.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlaySoundEntityEffectSystem : EntityEffectSystem<TransformComponent, PlaySound>
{
    [Dependency] private SharedAudioSystem _audio = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<PlaySound> args)
    {
        if (args.Effect.Predicted && args.User != null)
        {
            _audio.PlayPredicted(args.Effect.Sound, entity, args.User);
        }
        else
        {
            _audio.PlayPvs(args.Effect.Sound, entity);
        }
    }
}

public sealed partial class PlaySound : EntityEffectBase<PlaySound>
{
    /// <summary>
    /// The container entity to spawn.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("Weh");

    /// <summary>
    /// Whether the sound is predicted.
    /// Predicted audio requires a user passed in.
    /// </summary>
    [DataField]
    public bool Predicted = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => string.Empty;
}
