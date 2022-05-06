namespace Content.Shared.Sticky.Events;

public sealed class HasEntityStuckOnComponentTestAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The popup message that appears when it fails the test
    /// </summary>
    public string FailPopupMessage { get; }

    /// <summary>
    /// The user that raised the event
    /// </summary>
    public EntityUid User { get; }

    public HasEntityStuckOnComponentTestAttemptEvent(EntityUid user , string failPopupMessage = "event-sticky-has-things-stuck-on")
    {
        User = user;
        FailPopupMessage = failPopupMessage;
    }
}
