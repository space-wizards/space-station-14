using System.Collections.Immutable;
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
    public ImmutableHashSet<ListingDataWithCostModifiers> GetAvailableListings(EntityUid buyer, Entity<StoreComponent?> store)
    {
        var listings = new HashSet<ListingDataWithCostModifiers>();

        if (!Resolve(store.Owner, ref store.Comp))
            return listings.ToImmutableHashSet();

        var defaultListings = GetAvailableListingIDs(buyer, store).ToList();

        foreach (var listing in defaultListings)
        {
            if (!store.Comp.ListingsModifiers.TryGetValue(listing, out var resultListing))
                resultListing = new ListingDataWithCostModifiers(Proto.Index(listing));

            if (!CheckListingConditions(resultListing, buyer, store.Owner, store.Comp.Categories))
                continue;

            listings.Add(resultListing);
        }

        return listings.ToImmutableHashSet();
    }

    /// <summary>
    /// Gets the available listings for a store. COntains onl
    /// </summary>
    /// <param name="buyer">Either the account owner, user, or an inanimate object (e.g., surplus bundle)</param>
    /// <param name="store">Store to get all listings from.</param>
    /// <returns>The available listings.</returns>
    public IEnumerable<ProtoId<ListingPrototype>> GetAvailableListingIDs(EntityUid buyer, Entity<StoreComponent?> store)
    {
        if (!Resolve(store.Owner, ref store.Comp))
            return Array.Empty<ProtoId<ListingPrototype>>();

        return GetAvailableListingIDs(buyer, store.Comp.Categories, store.Owner);
    }

    /// <summary>
    /// Gets the available listings for a user given an overall set of listings and categories to filter by.
    /// </summary>
    /// <param name="buyer">Either the account owner, user, or an inanimate object (e.g., surplus bundle)</param>
    /// <param name="categories">What categories to filter by.</param>
    /// <param name="storeEntity">The physial entity of the store. Can be null.</param>
    /// <returns>The available listings.</returns>
    public IEnumerable<ProtoId<ListingPrototype>> GetAvailableListingIDs(
        EntityUid buyer,
        HashSet<ProtoId<StoreCategoryPrototype>> categories,
        EntityUid? storeEntity = null
    )
    {
        var listings = GetAllListings();

        foreach (var listingId in listings)
        {
            var listing = Proto.Index(listingId);

            if (!CheckListingConditions(listing, buyer, storeEntity, categories))
                continue;

            yield return listing;
        }
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

    private bool TryGetListing(
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
}
