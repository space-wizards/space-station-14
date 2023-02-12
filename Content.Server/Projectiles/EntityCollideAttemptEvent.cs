public sealed class ProjectileCollideAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Target;

    public ProjectileCollideAttemptEvent(EntityUid target)
    {
        Target = target;
    }
}
