using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.StoreDiscount.Components;

/// <summary>
/// Partner-component for adding discounts functionality to StoreSystem using StoreDiscountSystem.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoreDiscountComponent : Component
{
    /// <summary>
    /// Discounts for items in <see cref="ListingData"/>.
    /// </summary>
    [ViewVariables, DataField]
    public IReadOnlyCollection<StoreDiscountData> Discounts = Array.Empty<StoreDiscountData>();
}

/// <summary>
/// Container for listing item discount state.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class StoreDiscountData
{
    /// <summary>
    /// Id of listing item to be discounted.
    /// </summary>
    [DataField("listingId")]
    public ProtoId<ListingPrototype> ListingId = default!;

    /// <summary>
    /// Amount of discounted items. Each buy will decrement this counter.
    /// </summary>
    [DataField("count")]
    public int Count;

    /// <summary>
    /// Map of currencies to flat amount of discount.
    /// </summary>
    [DataField("discountAmountByCurrency")]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> DiscountAmountByCurrency = new();
}
