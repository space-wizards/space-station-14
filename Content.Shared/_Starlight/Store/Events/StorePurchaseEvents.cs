using Robust.Shared.Serialization;

namespace Content.Shared.Store.Events;

/// <summary>
/// Event raised when a store purchase is attempted.
/// Systems can subscribe to this event to potentially cancel the purchase.
/// </summary>
[ByRefEvent]
public struct StorePurchaseAttemptEvent
{
    public readonly string ListingId;
    public readonly EntityUid StoreEntity;
    public readonly EntityUid Buyer;
    
    /// <summary>
    /// Whether to cancel the purchase.
    /// Set to true to prevent the purchase from proceeding.
    /// </summary>
    public bool Cancel = false;
    
    public StorePurchaseAttemptEvent(string listingId, EntityUid storeEntity, EntityUid buyer)
    {
        ListingId = listingId;
        StoreEntity = storeEntity;
        Buyer = buyer;
    }
}

/// <summary>
/// Event raised when a store purchase is completed.
/// Systems can subscribe to this event to perform actions after a purchase.
/// </summary>
[ByRefEvent]
public struct StorePurchaseCompletedEvent
{
    public readonly string ListingId;
    public readonly EntityUid StoreEntity;
    public readonly EntityUid Buyer;
    
    public StorePurchaseCompletedEvent(string listingId, EntityUid storeEntity, EntityUid buyer)
    {
        ListingId = listingId;
        StoreEntity = storeEntity;
        Buyer = buyer;
    }
}
