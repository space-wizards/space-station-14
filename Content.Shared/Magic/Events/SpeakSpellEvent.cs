namespace Content.Shared.Magic.Events;

[ByRefEvent]
public readonly struct SpeakSpellEvent
{
    public readonly EntityUid Performer;
    public readonly string Speech;

    public SpeakSpellEvent(EntityUid performer, string speech)
    {
        Performer = performer;
        Speech = speech;
    }
}
