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

    public HasEntityStuckOnComponentTestAttemptEvent(EntityUid user , string failPopupMessage = "")
    {
        User = user;
        FailPopupMessage = failPopupMessage;
    }
}
