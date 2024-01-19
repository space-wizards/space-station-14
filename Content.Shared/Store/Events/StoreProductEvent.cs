namespace Content.Shared.Store.Events;

public record struct StoreProductEvent(EntityUid Purchaser, ListingData Listing, object? Ev);
