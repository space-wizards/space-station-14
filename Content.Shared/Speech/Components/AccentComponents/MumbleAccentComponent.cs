using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components.AccentComponents;

/// <summary>
/// Hmmfff!
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MumbleAccentComponent : Component
{
    /// <summary>
    /// This modifies the audio parameters of emote sounds, screaming, laughing, etc.
    /// By default, it reduces the volume and distance of emote sounds.
    /// </summary>
    [DataField]
    public AudioParams EmoteAudioParams = AudioParams.Default.WithVolume(-8f).WithMaxDistance(5);
}
