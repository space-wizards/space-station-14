using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Starlight.TTS;

public sealed class AnnouncementSpokeEvent : EntityEventArgs
{
    public Filter Source { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? AnnounceVoice { get; set; } = null!;
    public SoundSpecifier? AnnouncementSound { get; set; } = null!;
}
