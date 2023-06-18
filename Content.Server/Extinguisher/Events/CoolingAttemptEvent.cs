namespace Content.Server.Extinguisher.Events;

/// <summary>
///     Checks that entity can be cooled.
///     Raised twice: before do_after and after to check that entity still valid.
/// </summary>
public sealed class CoolingAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Extinguisher;

    public CoolingAttemptEvent(EntityUid user, EntityUid extinguisher)
    {
        User = user;
        Extinguisher = extinguisher;
    }
}
