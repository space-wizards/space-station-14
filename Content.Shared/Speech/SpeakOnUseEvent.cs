namespace Content.Shared.Speech;

[ByRefEvent]
public record struct SpeakOnUseEvent(EntityUid Performer);
