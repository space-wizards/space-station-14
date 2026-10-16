using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Plays a sound, either at this entity's coordinates or attached to it.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlaySoundEntityEffectSystem : EntityEffectSystem<TransformComponent, PlaySound>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<PlaySound> args)
    {
        switch (args.Effect.Type)
        {
            case PlaySoundRecipients.Broadcast:
                if (_net.IsServer)
                    _audio.PlayGlobal(args.Effect.Sound, Filter.Broadcast(), false);
                break;
            case PlaySoundRecipients.Local:
                if (args.User != null)
                    _audio.PlayEntity(args.Effect.Sound, entity.Owner, args.User.Value);
                break;
            case PlaySoundRecipients.Pvs when args.Effect.Method == PlaySoundMethod.PlayEntity:
                _audio.PlayPredicted(args.Effect.Sound, entity.Owner, args.User);
                break;
            case PlaySoundRecipients.Pvs:
                _audio.PlayPredicted(args.Effect.Sound, Transform(entity).Coordinates, args.User);
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlaySound : EntityEffectBase<PlaySound>
{
    /// <summary>
    /// The sound to play when this effect is applied.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound { get; set; } = default!;

    /// <summary>
    /// Where the sound is played from.
    /// </summary>
    [DataField]
    public PlaySoundMethod Method = PlaySoundMethod.PlayCoordinates;

    /// <summary>
    /// Who the sound is played for.
    /// </summary>
    [DataField]
    public PlaySoundRecipients Type = PlaySoundRecipients.Pvs;
}

[Serializable, NetSerializable]
public enum PlaySoundMethod : byte
{
    /// <summary>
    /// The sound is played at the entity's coordinates.
    /// </summary>
    PlayCoordinates,

    /// <summary>
    /// The sound follows the entity around.
    /// </summary>
    PlayEntity,
}

[Serializable, NetSerializable]
public enum PlaySoundRecipients : byte
{
    /// <summary>
    /// The sound is played for everyone in PVS range.
    /// </summary>
    Pvs,

    /// <summary>
    /// The sound is played only on the local client.
    /// </summary>
    Local,

    /// <summary>
    /// The sound is played globally, without positional audio.
    /// </summary>
    Broadcast,
}
