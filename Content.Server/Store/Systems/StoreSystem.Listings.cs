using System.Linq;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem
{
    /// <summary>
    /// Refreshes all listings on a store.
    /// Do not use if you don't know what you're doing.
    /// </summary>
    /// <param name="component">The store to refresh</param>
    public void RefreshAllListings(StoreComponent component)
    {
        component.Listings = GetAllListings().ToHashSet();
    }

    /// <summary>
    /// Gets all listings from a prototype.
    /// </summary>
    /// <returns>All the listings</returns>
    public IEnumerable<ListingData> GetAllListings()
    {
        var prototypes = _proto.EnumeratePrototypes<ListingPrototype>();
        return prototypes.Select(x => new ListingData(x));
    }

    /// <summary>
    /// Adds a listing from an Id to a store
    /// </summary>
    /// <param name="component">The store to add the listing to</param>
    /// <param name="listingId">The id of the listing</param>
    /// <returns>Whether or not the listing was added successfully</returns>
    public bool TryAddListing(StoreComponent component, string listingId)
    {
        if (!_proto.TryIndex<ListingPrototype>(listingId, out var proto))
        {
            Log.Error("Attempted to add invalid listing.");
            return false;
        }

        return TryAddListing(component, proto);
    }

    /// <summary>
    /// Adds a listing to a store
    /// </summary>
    /// <param name="component">The store to add the listing to</param>
    /// <param name="listing">The listing</param>
    /// <returns>Whether or not the listing was add successfully</returns>
    public bool TryAddListing(StoreComponent component, ListingPrototype listing)
    {
        return component.Listings.Add(listing);
    }

    /// <summary>
    /// Gets the available listings for a store
    /// </summary>
    /// <param name="buyer">Either the account owner, user, or an inanimate object (e.g., surplus bundle)</param>
    /// <param name="store"></param>
    /// <param name="component">The store the listings are coming from.</param>
    /// <returns>The available listings.</returns>
    public IEnumerable<ListingDataWithCostModifiers> GetAvailableListings(EntityUid buyer, EntityUid store, StoreComponent component)
    {
        return GetAvailableListings(buyer, null, component.Categories, store);
    }

    /// <summary>
    /// Gets the available listings for a user given an overall set of listings and categories to filter by.
    /// </summary>
    /// <param name="buyer">Either the account owner, user, or an inanimate object (e.g., surplus bundle)</param>
    /// <param name="listings">All of the listings that are available. If null, will just get all listings from the prototypes.</param>
    /// <param name="categories">What categories to filter by.</param>
    /// <param name="storeEntity">The physial entity of the store. Can be null.</param>
    /// <returns>The available listings.</returns>
    public IEnumerable<ListingDataWithCostModifiers> GetAvailableListings(
        EntityUid buyer,
        HashSet<ListingDataWithCostModifiers>? listings,
        HashSet<ProtoId<StoreCategoryPrototype>> categories,
        EntityUid? storeEntity = null
    )
    {
        if (listings == null)
        {
            var withModifiers = GetAllListings()
                                .Select(x => new ListingDataWithCostModifiers(x))
                                .ToHashSet();
            RaiseLocalEvent(new ListingItemsInitializingEvent(withModifiers, storeEntity));
            listings = withModifiers;
        }

        foreach (var listing in listings)
        {
            if (!ListingHasCategory(listing, categories))
                continue;

            if (listing.Conditions != null)
            {
                var args = new ListingConditionArgs(buyer, storeEntity, listing, EntityManager);
                var conditionsMet = true;

                foreach (var condition in listing.Conditions)
                {
                    if (!condition.Condition(args))
                    {
                        conditionsMet = false;
                        break;
                    }
                }

                if (!conditionsMet)
                    continue;
            }

            yield return listing;
        }
    }

    /// <summary>
    /// Checks if a listing appears in a list of given categories
    /// </summary>
    /// <param name="listing">The listing itself.</param>
    /// <param name="categories">The categories to check through.</param>
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
}

public sealed class ListingItemsInitializingEvent
{
    public ListingItemsInitializingEvent(IReadOnlyCollection<ListingDataWithCostModifiers> listingData, EntityUid? storeOwner)
    {
        ListingData = listingData;
        StoreOwner = storeOwner;
    }

    public ListingItemsInitializingEvent(ListingDataWithCostModifiers listingData, EntityUid? storeOwner)
    {
        ListingData = new HashSet<ListingDataWithCostModifiers> { listingData };
        StoreOwner = storeOwner;
    }

    public IReadOnlyCollection<ListingDataWithCostModifiers> ListingData { get; }
    public EntityUid? StoreOwner { get; }
}
