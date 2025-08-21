using Content.Shared._Starlight.Language;

namespace Content.Server.Starlight.TTS;

public sealed class RadioSpokeEvent : EntityEventArgs
{
    public EntityUid Source { get; set; }
    public string Message { get; set; } = null!;
    public LanguagePrototype Language = null!;
    public EntityUid[] Receivers { get; set; } = null!;
}
