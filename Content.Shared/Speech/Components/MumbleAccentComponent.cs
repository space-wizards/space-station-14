using Robust.Shared.Audio;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Hmmfff!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(MumbleAccentSystem))]
public sealed partial class MumbleAccentComponent : BaseAccentComponent
{
    /// <summary>
    /// This modifies the audio parameters of emote sounds, screaming, laughing, etc.
    /// By default, it reduces the volume and distance of emote sounds.
    /// </summary>
    [DataField]
    public AudioParams EmoteAudioParams = AudioParams.Default.WithVolume(-8f).WithMaxDistance(5);
}
