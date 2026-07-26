namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for store purchase and refund events.
/// Used with <c>LogType.StorePurchase</c> and <c>LogType.StoreRefund</c>.
/// </summary>
/// <param name="ListingId">Listing ID from the store catalogue.</param>
/// <param name="ProductPrototype">
/// Prototype ID of the spawned entity product, or null for action/event listings
/// that do not spawn an entity.
/// </param>
/// <param name="ProductDisplayName">Snapshot display name for historical context.</param>
/// <param name="Costs">All currency/amount pairs paid for this purchase.</param>
public sealed record StorePurchaseLogPayload(
    string ListingId,
    string? ProductPrototype,
    string ProductDisplayName,
    IReadOnlyList<StoreCostSnapshot> Costs) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}

/// <summary>
/// A single currency/amount pair from a store purchase.
/// </summary>
/// <param name="CurrencyPrototype">Currency prototype ID, e.g. <c>"TelecrystalCurrency"</c>.</param>
/// <param name="Amount">Amount paid as <c>FixedPoint2.Int()</c>.</param>
public sealed record StoreCostSnapshot(
    string CurrencyPrototype,
    int Amount);
