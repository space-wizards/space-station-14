using Robust.Shared.Audio;

namespace Content.Shared.Administration.Managers.Bwoink.Features;

/// <summary>
/// If this is on a channel, the sound specified within this will play for the receiver
/// </summary>
public sealed partial class SoundOnMessage : BwoinkChannelFeature
{
    [DataField(required: true)]
    public SoundPathSpecifier Sound { get; set; }

    /// <summary>
    /// If true, allows managers to check a "silent" tickbox that (you guessed it) makes a message NOT make a sound.
    /// </summary>
    [DataField]
    public bool AllowSilent { get; set; } = true;
}
