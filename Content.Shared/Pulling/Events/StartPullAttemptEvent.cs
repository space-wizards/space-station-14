namespace Content.Shared.Pulling.Events;

/// <summary>
///     Directed event raised on the puller to see if it can start pulling something.
/// </summary>
public sealed class StartPullAttemptEvent(EntityUid puller, EntityUid pulled) : CancellableEntityEventArgs
{
    public EntityUid Puller { get; } = puller;
    public EntityUid Pulled { get; } = pulled;
}
