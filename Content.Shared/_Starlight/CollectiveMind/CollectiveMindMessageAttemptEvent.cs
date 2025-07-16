namespace Content.Shared.CollectiveMind;

public sealed class CollectiveMindMessageAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    ///     The entity sending the message.
    /// </summary>
    public EntityUid Entity { get; }

    /// <summary>
    ///     The message being sent.
    ///     Modify this to apply effects to the text.
    /// </summary>
    public string Message { get; set; }

    public CollectiveMindMessageAttemptEvent(EntityUid entity, string message)
    {
        Entity = entity;
        Message = message;
    }
}
