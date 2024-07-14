using Content.Shared.Store;
using Robust.Shared.GameStates;

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
