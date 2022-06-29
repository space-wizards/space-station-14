namespace Content.Shared.Item;

/// <summary>
/// Raised on the user after they do any sort of interaction with an item,
/// useful for when you want a component on the user to do something to the item.
/// E.g. forensics, disease, etc.
/// </summary>
public sealed class UserInteractedWithItemEvent : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Item;
    public UserInteractedWithItemEvent(EntityUid user, EntityUid item)
    {
        User = user;
        Item = item;
    }
}
