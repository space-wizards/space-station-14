namespace Content.Shared.DeviceLinking.Events;

public sealed class NewLinkEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly EntityUid Sink;
    public readonly EntityUid? User;
    public readonly string SourcePort;
    public readonly string SinkPort;

    public NewLinkEvent(EntityUid? user, EntityUid source, string sourcePort, EntityUid sink, string sinkPort)
    {
        User = user;
        Source = source;
        SourcePort = sourcePort;
        Sink = sink;
        SinkPort = sinkPort;
    }
}
