namespace Content.Shared.Magic.Events;

[ByRefEvent]
public readonly struct SpeakSpellEvent(EntityUid performer, string speech)
{
    public readonly EntityUid Performer = performer;
    public readonly string Speech = speech;
}
