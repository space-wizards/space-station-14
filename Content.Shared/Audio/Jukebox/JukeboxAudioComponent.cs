namespace Content.Shared.Audio.Jukebox;

/// <summary>
/// Automatically attached to audio stream entities spawned by jukeboxes.
/// Used to notify the jukebox when the audio entity is deleted.
/// </summary>
[RegisterComponent]
public sealed partial class JukeboxAudioComponent : Component
{
    /// <summary>
    /// The jukebox entity that owns this audio entity.
    /// </summary>
    [DataField]
    public EntityUid Jukebox;
}
