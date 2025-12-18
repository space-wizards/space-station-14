using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.StoreDiscount.Components;

/// <summary>
/// Partner-component for adding discounts functionality to StoreSystem using StoreDiscountSystem.
/// </summary>
[RegisterComponent]
public sealed partial class StoreDiscountComponent : Component
{
    /// <summary>
    /// Discounts for items in <see cref="ListingData"/>.
    /// </summary>
    [ViewVariables, DataField]
    public IReadOnlyList<StoreDiscountData> Discounts = Array.Empty<StoreDiscountData>();
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
    [DataField(required: true)]
    public ProtoId<ListingPrototype> ListingId;

    /// <summary>
    /// Amount of discounted items. Each buy will decrement this counter.
    /// </summary>
    [DataField]
    public int Count;

    /// <summary>
    /// Discount category that provided this discount.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DiscountCategoryPrototype> DiscountCategory;

    /// <summary>
    /// Map of currencies to flat amount of discount.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> DiscountAmountByCurrency = new();
}
