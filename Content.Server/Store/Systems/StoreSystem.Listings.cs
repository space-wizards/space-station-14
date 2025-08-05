using System.Diagnostics.CodeAnalysis;
using Content.Shared.Mind;
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
        var previousState = component.FullListingsCatalog;
        var newState = GetAllListings();
        // if we refresh list with existing cost modifiers - they will be removed,
        // need to restore them
        if (previousState.Count != 0)
        {
            foreach (var previousStateListingItem in previousState)
            {
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
        
        // STARLIGHT: Check if a rift has been destroyed and update the listing accordingly
        // This ensures the rift listing remains unavailable even after reopening the uplink
        _revSupplyRift.CheckRiftDestroyedAndUpdateListing(component);
    }

    /// <summary>
    /// Gets all listings from a prototype.
    /// </summary>
    /// <returns>All the listings</returns>
    public HashSet<ListingDataWithCostModifiers> GetAllListings()
    {
        var clones = new HashSet<ListingDataWithCostModifiers>();
        foreach (var prototype in _proto.EnumeratePrototypes<ListingPrototype>())
        {
            clones.Add(new ListingDataWithCostModifiers(prototype));
        }

        return clones;
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
        return component.FullListingsCatalog.Add(new ListingDataWithCostModifiers(listing));
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
        return GetAvailableListings(buyer, component.FullListingsCatalog, component.Categories, store);
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
        IReadOnlyCollection<ListingDataWithCostModifiers>? listings,
        HashSet<ProtoId<StoreCategoryPrototype>> categories,
        EntityUid? storeEntity = null
    )
    {
        listings ??= GetAllListings();

        foreach (var listing in listings)
        {
            if (!ListingHasCategory(listing, categories))
                continue;

            // Starlight Start: Reset the unavailable flag before checking conditions
            listing.Unavailable = false;

            if (listing.Conditions != null)
            {
                var args = new ListingConditionArgs(GetBuyerMind(buyer), storeEntity, listing, EntityManager);
                bool hasStockLimitedCondition = false;
                bool allConditionsMet = true;
                
                // First pass: check if this listing has a StockLimitedListingCondition
                foreach (var condition in listing.Conditions)
                {
                    if (condition is Content.Shared.Store.Conditions.StockLimitedListingCondition)
                    {
                        hasStockLimitedCondition = true;
                        break;
                    }
                }
                
                // Second pass: check all conditions
                foreach (var condition in listing.Conditions)
                {
                    if (!condition.Condition(args))
                    {
                        // If this is a StockLimitedListingCondition, we want to show the item but mark it as unavailable
                        if (condition is Content.Shared.Store.Conditions.StockLimitedListingCondition)
                        {
                            listing.Unavailable = true;
                        }
                        else if (!hasStockLimitedCondition)
                        {
                            // For other conditions, if they return false and this isn't a stock-limited item,
                            // we skip this listing entirely
                            allConditionsMet = false;
                            break;
                        }
                    }
                }
                
                // Skip this listing if conditions aren't met and it's not a stock-limited item
                if (!allConditionsMet && !hasStockLimitedCondition)
                {
                    goto NextListing;
                }
            }

            yield return listing;
            
            NextListing:
            continue;
            // Starlight End
        }
    }

    /// <summary>
    /// Returns the entity's mind entity, if it has one, to be used for listing conditions.
    /// If it doesn't have one, or is a mind entity already, it returns itself.
    /// </summary>
    /// <param name="buyer">The buying entity.</param>
    public EntityUid GetBuyerMind(EntityUid buyer)
    {
        if (!HasComp<MindComponent>(buyer) && _mind.TryGetMind(buyer, out var buyerMind, out var _))
            return buyerMind;

        return buyer;
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

    private bool TryGetListing(IReadOnlyCollection<ListingDataWithCostModifiers> collection, string listingId, [MaybeNullWhen(false)] out ListingDataWithCostModifiers found)
    {
        foreach(var current in collection)
        {
            if (current.ID == listingId)
            {
                found = current;
                return true;
            }
        }

        found = null!;
        return false;
    }
}
