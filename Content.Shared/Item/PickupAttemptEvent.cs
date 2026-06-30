namespace Content.Shared.Item;

/// <summary>
/// Raised on a *mob* when it tries to pickup something.
/// IMPORTANT: Attempt event subscriptions should not be doing any state changes like throwing items, opening UIs, playing sounds etc!
/// </summary>
public sealed class PickupAttemptEvent : BasePickupAttemptEvent
{
    public PickupAttemptEvent(EntityUid user, EntityUid item, bool showPopup) : base(user, item, showPopup) { }
}

/// <summary>
/// Raised directed at entity being picked up when someone tries to pick it up.
/// IMPORTANT: Attempt event subscriptions should not be doing any state changes like throwing items, opening UIs, playing sounds etc!
/// </summary>
public sealed class GettingPickedUpAttemptEvent : BasePickupAttemptEvent
{
    public GettingPickedUpAttemptEvent(EntityUid user, EntityUid item, bool showPopup) : base(user, item, showPopup) { }
}

[Virtual]
public class BasePickupAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The mob that is picking up the item.
    /// </summary>
    public readonly EntityUid User;
    /// <summary>
    /// The item being picked up.
    /// </summary>

    public readonly EntityUid Item;

    /// <summary>
    /// Whether or not to show a popup message to the player telling them why the attempt was cancelled.
    /// This should be true when this event is raised during interactions, and false when it is raised
    /// for disabling verbs or similar that do not do the actual pickup.
    /// </summary>
    public bool ShowPopup;

    public BasePickupAttemptEvent(EntityUid user, EntityUid item, bool showPopup)
    {
        User = user;
        Item = item;
        ShowPopup = showPopup;
    }
}
