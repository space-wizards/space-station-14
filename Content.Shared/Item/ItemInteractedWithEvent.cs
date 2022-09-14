namespace Content.Shared.Item;

/// <summary>
/// Raised on the item after they do any sort of interaction with an item,
/// useful for when you want a component on the user to do something to the user
/// E.g. forensics, disease, etc.
/// </summary>
public sealed class ItemInteractedWithEvent : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Item;
    public ItemInteractedWithEvent(EntityUid user, EntityUid item)
    {
        User = user;
        Item = item;
    }
}
