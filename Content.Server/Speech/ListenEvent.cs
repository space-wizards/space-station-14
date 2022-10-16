namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;

    public ListenEvent(string message, EntityUid source)
    {
        Message = message;
        Source = source;
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;

    public ListenAttemptEvent(string message, EntityUid source)
    {
        Message = message;
        Source = source;
    }
}
