namespace Content.Shared.Sticky.Events;

public sealed class StickyComponentTestAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The popup message that appears when it fails the test
    /// </summary>
    public string FailPopupMessage { get; }

    /// <summary>
    /// The user that raised the event
    /// </summary>
    public EntityUid User { get; }

    public StickyComponentTestAttemptEvent(EntityUid user, string failPopupMessage = "event-sticky-is-stuck-on")
    {
        FailPopupMessage = failPopupMessage;
        User = user;
    }
}
