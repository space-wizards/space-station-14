using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Plays a sound at the entity's coordinates.
/// Used by blood cultists when they are burned by holy water.
/// Can be used by other entities to play sounds at their coordinates.
/// </summary>
public sealed partial class PlaySoundEntityEffectSystem : EntityEffectSystem<TransformComponent, PlaySound>
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<PlaySound> args)
    {
        if (args.Effect.Sound == null)
            return;

        _audio.PlayPvs(args.Effect.Sound, entity, args.Effect.Sound.Params);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlaySound : EntityEffectBase<PlaySound>
{
    /// <summary>
    /// The sound to play.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
