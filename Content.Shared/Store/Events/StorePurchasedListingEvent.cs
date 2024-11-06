namespace Content.Shared.Store.Events;

public record struct StorePurchasedListingEvent(EntityUid Purchaser, ListingData Listing, EntityUid? Item, EntityUid? Action);
