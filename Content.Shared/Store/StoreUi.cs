using Content.Shared.FixedPoint;
using Content.Shared.MobState;
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
    public readonly EntityUid? Buyer;

    public readonly HashSet<ListingData> Listings;

    public readonly Dictionary<string, FixedPoint2> Balance;

    public StoreUpdateState(EntityUid? buyer, HashSet<ListingData> listings, Dictionary<string, FixedPoint2> balance)
    {
        Buyer = buyer;
        Listings = listings;
        Balance = balance;
    }
}

/// <summary>
/// initializes miscellaneous data about the store.
/// </summary>
[Serializable, NetSerializable]
public sealed class StoreInitializeState : BoundUserInterfaceState
{
    public readonly string Name;

    public StoreInitializeState(string name)
    {
        Name = name;
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

[Serializable, NetSerializable]
public sealed class StoreRequestWithdrawMessage : BoundUserInterfaceMessage
{
    public EntityUid Buyer;

    public string Currency;

    public int Amount;

    public StoreRequestWithdrawMessage(EntityUid buyer, string currency, int amount)
    {
        Buyer = buyer;
        Currency = currency;
        Amount = amount;
    }
}
