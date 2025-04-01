using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Base sound emitter which defines most of the data fields.
/// Accepts both single sounds and sound collections.
/// </summary>
public abstract partial class BaseEmitSoundComponent : Component
{
    /// <summary>
    /// The <see cref="SoundSpecifier"/> to play.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Play the sound at the position instead of parented to the source entity.
    /// Useful if the entity is deleted after.
    /// </summary>
    [DataField]
    public bool Positional;
}

/// <summary>
/// Represents the state of <see cref="BaseEmitSoundComponent"/>.
/// </summary>
/// <remarks>This is obviously very cursed, but since the BaseEmitSoundComponent is abstract, we cannot network it.
/// AutoGenerateComponentState attribute won't work here, and since everything revolves around inheritance for some fucking reason,
/// there's no better way of doing this.</remarks>
[Serializable, NetSerializable]
public struct EmitSoundComponentState(SoundSpecifier? sound) : IComponentState
{
    public SoundSpecifier? Sound { get; } = sound;
}
