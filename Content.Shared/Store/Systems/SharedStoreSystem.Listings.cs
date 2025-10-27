using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store.Systems;

public abstract partial class SharedStoreSystem
{
    /// <summary>
    /// Refreshes all listings on a store.
    /// Do not use if you don't know what you're doing.
    /// </summary>
    /// <param name="store">The store to refresh</param>
    /*public void RefreshAllListings(Entity<StoreComponent> store)
    {
        var (uid, component) = store;
        var previousState = component.FullListingsCatalog;
        var newState = GetAllListings();
        // if we refresh list with existing cost modifiers - they will be removed,
        // need to restore them
        if (previousState.Count != 0)
        {
            foreach (var previousStateListingItemId in previousState)
            {
                var previousStateListingItem = Proto.Index(previousStateListingItemId);

                if (!previousStateListingItem.IsCostModified
                    || !TryGetListing(newState, previousStateListingItem.ID, out var found))
                {
                    continue;
                }

                foreach (var (modifierSourceId, costModifier) in previousStateListingItem.CostModifiersBySourceId)
                {
                    found.AddCostModifier(modifierSourceId, costModifier);
                }
            }
        }

        component.FullListingsCatalog = newState;
        DirtyField(uid, component, nameof(StoreComponent.FullListingsCatalog));
        UpdateUi((store, component));
    }*/

    /// <summary>
    /// Gets all listings from a prototype.
    /// </summary>
    /// <returns>All the listings</returns>
    public List<ProtoId<ListingPrototype>> GetAllListings()
    {
        var prototypes = Proto.EnumeratePrototypes<ListingPrototype>();
        return prototypes.Select(prototype => (ProtoId<ListingPrototype>) prototype.ID).ToList();
    }

    /// <summary>
    /// Gets the available listings for a store
    /// </summary>
    /// <param name="buyer">Either the account owner, user, or an inanimate object (e.g., surplus bundle)</param>
    /// <param name="store">Store to get all listings from.</param>
    /// <returns>The available listings.</returns>
    public IEnumerable<ProtoId<ListingPrototype>> GetAvailableListings(EntityUid buyer, Entity<StoreComponent?> store)
    {
        if (!Resolve(store.Owner, ref store.Comp))
            return Array.Empty<ProtoId<ListingPrototype>>();

        return GetAvailableListings(buyer, store.Comp.Categories, store.Owner);
    }

    /// <summary>
    /// Gets the available listings for a user given an overall set of listings and categories to filter by.
    /// </summary>
    /// <param name="buyer">Either the account owner, user, or an inanimate object (e.g., surplus bundle)</param>
    /// <param name="listings">All of the listings that are available. If null, will just get all listings from the prototypes.</param>
    /// <param name="categories">What categories to filter by.</param>
    /// <param name="storeEntity">The physial entity of the store. Can be null.</param>
    /// <returns>The available listings.</returns>
    public IEnumerable<ProtoId<ListingPrototype>> GetAvailableListings(
        EntityUid buyer,
        HashSet<ProtoId<StoreCategoryPrototype>> categories,
        EntityUid? storeEntity = null
    )
    {
        var listings = GetAllListings();

        foreach (var listingId in listings)
        {
            var listing = Proto.Index(listingId);

            if (!ListingHasCategory(listing, categories))
                continue;

            if (listing.Conditions != null)
            {
                var args = new ListingConditionArgs(GetBuyerMind(buyer), storeEntity, listing, EntityManager);
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

    private bool TryGetListing(
        IReadOnlyCollection<ListingDataWithCostModifiers> collection,
        string listingId,
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
