using Robust.Shared.Audio;

namespace Content.Shared.Administration.Managers.Bwoink.Features;

/// <summary>
/// If this is on a channel, the sound specified within this will play for the receiver
/// </summary>
public sealed partial class SoundOnMessage : BwoinkChannelFeature
{
    [DataField(required: true)]
    public SoundPathSpecifier Sound { get; set; }
}
