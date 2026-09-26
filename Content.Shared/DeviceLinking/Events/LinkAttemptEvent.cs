namespace Content.Shared.DeviceLinking.Events;

[ByRefEvent]
public record struct LinkAttemptEvent(
    EntityUid? User,
    EntityUid Source,
    string SourcePort,
    EntityUid Sink,
    string SinkPort,
    bool Cancelled = false);
