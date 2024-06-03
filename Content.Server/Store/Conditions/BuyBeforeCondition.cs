using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Conditions;

public sealed partial class BuyBeforeCondition : ListingCondition
{
    /// <summary>
    ///     Required listing(s) needed to purchase before this listing is available
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<ListingPrototype>> Whitelist;

    /// <summary>
    ///     Listing(s) that if bought, block this purchase, if any.
    /// </summary>
    public HashSet<ProtoId<ListingPrototype>>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        if (!args.EntityManager.TryGetComponent<StoreComponent>(args.StoreEntity, out var storeComp))
            return false;

        var allListings = storeComp.Listings;

        var purchasesFound = false;

        if (Blacklist != null)
        {
            foreach (var blacklistListing in Blacklist)
            {
                foreach (var listing in allListings)
                {
                    if (listing.ID == blacklistListing.Id && listing.PurchaseAmount > 0)
                        return false;
                }
            }
        }

        foreach (var requiredListing in Whitelist)
        {
            foreach (var listing in allListings)
            {
                if (listing.ID == requiredListing.Id)
                {
                    purchasesFound = listing.PurchaseAmount > 0;
                    break;
                }
            }
        }

        return purchasesFound;
    }
}
