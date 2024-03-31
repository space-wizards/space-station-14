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
    public readonly HashSet<ListingData> Listings;

    public readonly Dictionary<string, FixedPoint2> Balance;

    public readonly bool ShowFooter;

    public readonly bool AllowRefund;

    public StoreUpdateState(HashSet<ListingData> listings, Dictionary<string, FixedPoint2> balance, bool showFooter, bool allowRefund)
    {
        Listings = listings;
        Balance = balance;
        ShowFooter = showFooter;
        AllowRefund = allowRefund;
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
    public StoreRequestUpdateInterfaceMessage()
    {
    }
}

[Serializable, NetSerializable]
public sealed class StoreBuyListingMessage : BoundUserInterfaceMessage
{
    public ListingData Listing;

    public StoreBuyListingMessage(ListingData listing)
    {
        Listing = listing;
    }
}

[Serializable, NetSerializable]
public sealed class StoreRequestWithdrawMessage : BoundUserInterfaceMessage
{
    public string Currency;

    public int Amount;

    public StoreRequestWithdrawMessage(string currency, int amount)
    {
        Currency = currency;
        Amount = amount;
    }
}

/// <summary>
///     Used when the refund button is pressed
/// </summary>
[Serializable, NetSerializable]
public sealed class StoreRequestRefundMessage : BoundUserInterfaceMessage
{

}
