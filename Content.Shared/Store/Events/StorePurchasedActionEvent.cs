namespace Content.Shared.Store.Events;

public record struct StorePurchasedActionEvent(EntityUid Purchaser, EntityUid Action);
