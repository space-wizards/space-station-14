namespace Content.Shared.Item;

/// <summary>
///     Raised on a *mob* when it tries to pickup something
/// </summary>
public sealed class PickupAttemptEvent(EntityUid user, EntityUid item, bool physicalAttempt = false)
    : BasePickupAttemptEvent(user, item, physicalAttempt);

/// <summary>
///     Raised directed at entity being picked up when someone tries to pick it up
/// </summary>
public sealed class GettingPickedUpAttemptEvent(EntityUid user, EntityUid item, bool physicalAttempt = false)
    : BasePickupAttemptEvent(user, item, physicalAttempt);

[Virtual]
public class BasePickupAttemptEvent(EntityUid user, EntityUid item, bool physicalAttempt = false)
    : CancellableEntityEventArgs
{
    public readonly EntityUid User = user;
    public readonly EntityUid Item = item;

    /// <summary>
    /// String describing the reason the pickup failed.
    ///
    /// Will not always be displayed to the user, depending on who raised the event.
    /// </summary>
    public string? Reason;

    /// <summary>
    /// Whether or not the event is raised in response to a physical action taken.
    /// If false, in-game side effects should not occur in response to this event.
    /// </summary>
    public bool PhysicalAttempt = physicalAttempt;
}
