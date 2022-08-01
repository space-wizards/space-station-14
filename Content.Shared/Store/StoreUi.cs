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

    public Dictionary<string, float> Currency;

    public HashSet<ListingData> Listings;
    public StoreUpdateState(EntityUid? buyer, HashSet<ListingData> listings, Dictionary<string, float> currency)
    {
        Buyer = buyer;
        Listings = listings;
        Currency = currency;
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
