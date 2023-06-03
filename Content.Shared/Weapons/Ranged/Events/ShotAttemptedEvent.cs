namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a gun when someone is attempting to shoot it.
/// Cancel this event to prevent it from shooting.
/// </summary>
public sealed class ShotAttemptedEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The user attempting to shoot the gun.
    /// </summary>
    public EntityUid User;
}
