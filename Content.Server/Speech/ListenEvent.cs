namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    public readonly bool isLOOC; //the only reason isLOOC exists here is for the telephone system to detect it and it is not related to the normal looc messages 

    public ListenEvent(string message, EntityUid source, bool isLOOC = false)
    {
        Message = message;
        Source = source;
        this.isLOOC = isLOOC;
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
