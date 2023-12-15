namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a gun when someone is attempting to shoot it.
/// Cancel this event to prevent it from shooting.
/// </summary>
[ByRefEvent]
public record struct ShotAttemptedEvent
{
    /// <summary>
    /// The user attempting to shoot the gun.
    /// </summary>
    public EntityUid User;

    /// <summary>
    /// The gun being shot.
    /// </summary>
    public EntityUid Used;

    public bool Cancelled { get; private set; }

    /// </summary>
    /// Prevent the gun from shooting
    /// </summary>
    public void Cancel()
    {
        Cancelled = true;
    }

    /// </summary>
    /// Allow the gun to shoot again, only use if you know what you are doing
    /// </summary>
    public void Uncancel()
    {
        Cancelled = false;
    }
}
