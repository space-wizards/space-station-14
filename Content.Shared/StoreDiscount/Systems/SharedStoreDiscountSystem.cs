using System.Diagnostics.CodeAnalysis;
using Content.Shared.Store;
using Content.Shared.Store.Systems;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.StoreDiscount.Systems;

public abstract class SharedStoreDiscountSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;

    protected static readonly ProtoId<StoreCategoryPrototype> DiscountedStoreCategoryPrototypeKey = "DiscountedItems";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
    }

    /// <summary> Decrements discounted item count, removes discount modifier and category, if counter reaches zero. </summary>
    private void OnBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        var (storeId, purchasedItem) = ev;
        if (!TryComp<StoreDiscountComponent>(storeId, out var discountsComponent))
            return;

        // find and decrement discount count for item, if there is one.
        if (!TryGetDiscountData(discountsComponent.Discounts, purchasedItem, out var discountData) || discountData.Count == 0)
            return;

        discountData.Count--;

        // if there were discounts, but they are all bought up now - restore state: remove modifier and remove store category
        if (discountData.Count <= 0)
        {
            // TODO STORE
            // Since we are running code in prediction, it runs multiple times, and that causes a store to remove a discount on the first tick
            // and then on all next ticks there will be no discount anymore, so it causes client to mispredict
            // and sell the last discounted item in stock for full price. AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            if (_netMan.IsServer)
                purchasedItem.RemoveCostModifier(discountData.DiscountCategory);

            purchasedItem.Categories.Remove(DiscountedStoreCategoryPrototypeKey);
        }

        Dirty(storeId, discountsComponent);
    }

    protected static bool TryGetDiscountData(
        IReadOnlyList<StoreDiscountData> discounts,
        ListingDataWithCostModifiers purchasedItem,
        [NotNullWhen(true)] out StoreDiscountData? discountData
    )
    {
        discountData = null;

        foreach (var current in discounts)
        {
            if (current.ListingId != purchasedItem.ID)
                continue;

            discountData = current;
            return true;
        }

        return false;
    }
}
