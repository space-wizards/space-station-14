using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Store;

[Serializable, NetSerializable]
public enum StoreUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class StoreUpdateState : BoundUserInterfaceState
{
    public EntityUid? Buyer;

    public HashSet<ListingData> Listings;

    public Dictionary<string, FixedPoint2> Balance;

    public StoreUpdateState(EntityUid? buyer, HashSet<ListingData> listings, Dictionary<string, FixedPoint2> balance)
    {
        Buyer = buyer;
        Listings = listings;
        Balance = balance;
    }
}

[Serializable, NetSerializable]
public sealed class StoreRequestUpdateInterfaceMessage : BoundUserInterfaceMessage
{
    public EntityUid CurrentBuyer;

    public StoreRequestUpdateInterfaceMessage(EntityUid currentBuyer)
    {
        CurrentBuyer = currentBuyer;
    }
}

[Serializable, NetSerializable]
public sealed class StoreBuyListingMessage : BoundUserInterfaceMessage
{
    public EntityUid Buyer;

    public ListingData Listing;

    public StoreBuyListingMessage(EntityUid buyer, ListingData listing)
    {
        Buyer = buyer;
        Listing = listing;
    }
}
