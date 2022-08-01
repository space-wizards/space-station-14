using Content.Server.Store.Components;
using Content.Shared.Store;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : EntitySystem
{
    public void RefreshAllListings(StoreComponent component)
    {
        var allListings = _proto.EnumeratePrototypes<ListingPrototype>();

        component.Listings = new();

        foreach (var listing in allListings)
        {
            ListingData data = listing;
            component.Listings.Add(data);
        }
    }

    public bool TryAddListing(StoreComponent component, string listingId)
    {
        if (!_proto.TryIndex<ListingPrototype>(listingId, out var proto))
        {
            Logger.Error("Attempted to add invalid listing.");
            return false;
        }
        return TryAddListing(component, proto);
    }

    public bool TryAddListing(StoreComponent component, ListingData listing)
    {
        return component.Listings.Add(listing);
    }

    public IEnumerable<ListingData> GetAvailableListings(EntityUid user, StoreComponent component)
    {
        foreach (var listing in component.Listings)
        {
            if (!ListingHasCategory(listing, component.Categories))
                continue;

            if (listing.Conditions != null)
            {
                var args = new ListingConditionArgs(user, listing, EntityManager);
                var conditionsMet = true;

                foreach (var condition in listing.Conditions)
                    if (!condition.Condition(args))
                        conditionsMet = false;

                if (!conditionsMet)
                    continue;
            }

            yield return listing;
        }
    }

    public bool ListingHasCategory(ListingData listing, HashSet<string> categories)
    {
        foreach (var cat in categories)
        {
            if (listing.Categories.Contains(cat))
                return true;
        }
        return false;
    }
}
