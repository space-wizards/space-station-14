namespace Content.Shared.Sticky.Events;

public sealed class StickySystemTestAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The popup message that appears when it fails the test
    /// </summary>
    public string FailPopupMessage { get; }

    public EntityUid Subject { get; }

    public EntityUid User { get; }

    public StickySystemTestAttemptEvent(EntityUid subject, EntityUid user, string failPopupMessage = "event-sticky-general-fail")
    {
        FailPopupMessage = failPopupMessage;
        Subject = subject;
        User = user;
    }
}
