using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.StoreDiscount.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoreDiscountComponent : Component
{
    /// <summary>
    /// Discounts for items in <see cref="ListingData"/>.
    /// </summary>
    [ViewVariables, DataField]
    public StoreDiscountData[] Discounts = Array.Empty<StoreDiscountData>();
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class StoreDiscountData
{
    /// <summary>
    /// Id of listing item to be discounted.
    /// </summary>
    [DataField("listingId")]
    public string ListingId = default!;

    /// <summary>
    /// Amount of discounted items. Each buy will decrement this counter.
    /// </summary>
    [DataField("count")]
    public int Count;

    /// <summary>
    /// Map of currencies to flat amount of discount.
    /// </summary>
    [DataField("discountAmountByCurrency", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, CurrencyPrototype>))]
    public Dictionary<string, FixedPoint2> DiscountAmountByCurrency = new();
}
