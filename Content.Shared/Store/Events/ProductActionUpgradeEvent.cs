namespace Content.Shared.Store.Events;

public sealed class ProductActionUpgradeEvent : EntityEventArgs
{
    // Keep track of the action purchased EUID
    //      Also have it pass the right listing?
    //      If Fireball listed, the event will have the listing name of the Fireball Upgrade Event
    //      If Fireball upgrade listed, the event will still have the Fireball Upgrade event
    //      If Fireball can still level up, but not change, don't change out the entity
    // Fire event
    // Relevant Listener, which is an action upgrade in the s
}
