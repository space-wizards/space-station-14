using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
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
    public readonly HashSet<ListingDataWithCostModifiers> Listings;

    public readonly Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Balance;

    public readonly bool ShowFooter;

    public readonly bool AllowRefund;

    public readonly HashSet<ProtoId<StoreCategoryPrototype>> Categories; // DS14

    public StoreUpdateState(HashSet<ListingDataWithCostModifiers> listings, Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> balance, bool showFooter, bool allowRefund, HashSet<ProtoId<StoreCategoryPrototype>> categories) // DS14
    {
        Listings = listings;
        Balance = balance;
        ShowFooter = showFooter;
        AllowRefund = allowRefund;
        Categories = categories; // DS14
    }
}

[Serializable, NetSerializable]
public sealed class StoreRequestUpdateInterfaceMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class StoreBuyListingMessage(ProtoId<ListingPrototype> listing) : BoundUserInterfaceMessage
{
    public ProtoId<ListingPrototype> Listing = listing;
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
