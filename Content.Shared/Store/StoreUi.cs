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
    public EntityUid Buyer;

    public Dictionary<string, float> Currency;

    public HashSet<ListingData> Listings;
    public StoreUpdateState(EntityUid buyer, Dictionary<string, float> currency, HashSet<ListingData> listings)
    {
        Buyer = buyer;
        Currency = currency;
        Listings = listings;
    }
}
