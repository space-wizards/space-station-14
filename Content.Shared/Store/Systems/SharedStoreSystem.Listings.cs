using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store.Systems;

public abstract partial class SharedStoreSystem
{
    /// <summary>
    /// Gets all listings from a prototype. Also takes into account <see cref="StoreComponent.ListingsModifiers"/> overrides.
    /// </summary>
    /// <returns>
    /// All listings as <see cref="ListingDataWithCostModifiers"/>.
    /// </returns>
    /// <remarks>
    /// Do not modify this collection directly, it won't work.
    /// Instead, add a new key and value to <see cref="StoreComponent.ListingsModifiers"/> and dirty it.
    /// </remarks>
    public HashSet<ListingDataWithCostModifiers> GetAvailableListings(
        EntityUid buyer,
        Entity<StoreComponent?> store,
        bool checkConditions = true)
    {
        var listings = new HashSet<ListingDataWithCostModifiers>();

        if (!Resolve(store.Owner, ref store.Comp))
            return listings;

        var defaultListings = GetAllListings();

        foreach (var listing in defaultListings)
        {
            if (!TryGetListing(store.Comp.ListingsModifiers, listing, out var resultListing))
                resultListing = new ListingDataWithCostModifiers(Proto.Index(listing));

            if (checkConditions && !CheckListingConditions(resultListing, buyer, store.Owner, store.Comp.Categories))
                continue;

            listings.Add(resultListing);
        }

        return listings;
    }

    private bool CheckListingConditions(
        ListingData listing,
        EntityUid buyer,
        EntityUid? store,
        HashSet<ProtoId<StoreCategoryPrototype>> categories)
    {
        if (!ListingHasCategory(listing, categories))
            return false;

        if (listing.Conditions == null)
            return true;

        var args = new ListingConditionArgs(GetBuyerMind(buyer), store, listing, EntityManager, Proto);
        var conditionsMet = true;

        foreach (var condition in listing.Conditions)
        {
            if (!condition.Condition(args))
            {
                conditionsMet = false;
                break;
            }
        }

        return conditionsMet;
    }

    /// <summary>
    /// Returns the entity's mind entity, if it has one, to be used for listing conditions.
    /// If it doesn't have one, or is a mind entity already, it returns itself.
    /// </summary>
    /// <param name="buyer">The buying entity.</param>
    public EntityUid GetBuyerMind(EntityUid buyer)
    {
        if (!HasComp<MindComponent>(buyer) && _mind.TryGetMind(buyer, out var buyerMind, out _))
            return buyerMind;

        return buyer;
    }

    /// <summary>
    /// Checks if a listingId appears in a list of given categories
    /// </summary>
    /// <returns>If the listing was present in one of the categories.</returns>
    public bool ListingHasCategory(ListingData listing, HashSet<ProtoId<StoreCategoryPrototype>> categories)
    {
        foreach (var cat in categories)
        {
            if (listing.Categories.Contains(cat))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all listings from a prototype.
    /// </summary>
    /// <returns>All the listings</returns>
    private List<ProtoId<ListingPrototype>> GetAllListings()
    {
        var prototypes = Proto.EnumeratePrototypes<ListingPrototype>();
        return prototypes.Select(prototype => (ProtoId<ListingPrototype>) prototype.ID).ToList();
    }

    /// <summary>
    /// Adds a listing to the list and ensures that it has a unique ID.
    /// If there's already a listing with the same ID, replaces it.
    /// </summary>
    /// <returns></returns>
    public void EnsureListingUnique(List<ListingDataWithCostModifiers> collection, ListingDataWithCostModifiers listing)
    {
        // If modifier of this listing already exists, replace it.
        if (TryGetListing(collection, listing.ID, out _, out var modifiedIndex))
        {
            collection[modifiedIndex.Value] = listing;
        }
        else
        {
            collection.Add(listing);
        }
    }

    public bool TryGetListing(
        IReadOnlyCollection<ListingDataWithCostModifiers> collection,
        ProtoId<ListingPrototype> listingId,
        [NotNullWhen(true)] out ListingDataWithCostModifiers? found)
    {
        found = null;
        foreach(var current in collection)
        {
            if (current.ID != listingId)
                continue;

            found = current;
            return true;
        }

        return false;
    }

    public bool TryGetListing(
        List<ListingDataWithCostModifiers> collection,
        ProtoId<ListingPrototype> listingId,
        [NotNullWhen(true)] out ListingDataWithCostModifiers? found,
        [NotNullWhen(true)] out int? foundIndex)
    {
        found = null;
        foundIndex = null;
        for (var i = 0; i < collection.Count; i++)
        {
            var current = collection[i];
            if (current.ID != listingId)
                continue;

            found = current;
            foundIndex = i;
            return true;
        }

        return false;
    }
}
