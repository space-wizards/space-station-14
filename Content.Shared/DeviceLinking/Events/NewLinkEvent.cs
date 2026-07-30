namespace Content.Shared.DeviceLinking.Events;

[ByRefEvent]
public readonly record struct NewLinkEvent(
    EntityUid? User,
    EntityUid Source,
    string SourcePort,
    EntityUid Sink,
    string SinkPort);
