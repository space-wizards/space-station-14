using System.Linq;
using Content.Shared.Store.Components;
using Content.Shared.Store.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Store.Conditions;

[Serializable, NetSerializable]
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
    [DataField]
    public HashSet<ProtoId<ListingPrototype>>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var entMan = args.EntityManager;

        if (!entMan.TryGetComponent<StoreComponent>(args.StoreEntity, out var storeComp))
            return false;

        var storeSystem = entMan.System<SharedStoreSystem>();
        var allListings = storeSystem.GetAvailableListings(args.Buyer, (args.StoreEntity.Value, storeComp), false).ToList();

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
