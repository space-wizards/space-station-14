namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for cargo order events.
/// Used with <c>LogType.CargoOrder</c>.
/// </summary>
/// <param name="OrderId">The unique order ID assigned by the cargo system.</param>
/// <param name="ProductPrototype"> Prototype ID</param>
/// <param name="ProductDisplayName"> Display name for historical context.</param>
/// <param name="Quantity">Number of items in the order.</param>
/// <param name="TotalCost">Total cost in credits (integer).</param>
/// <param name="Status">
/// Order lifecycle status: <c>"Inserted"</c>, <c>"Approved"</c>, <c>"Added"</c>,
/// <c>"AddedAndApproved"</c>, <c>"Denied"</c>, <c>"Cancelled"</c>.
/// </param>
/// <param name="Requester">Character name of the player who requested the order.</param>
/// <param name="Reason">Requester-supplied reason. Null if empty.</param>
/// <param name="Account">Cargo account this order was placed against.</param>
public sealed record CargoOrderLogPayload(
    int OrderId,
    string ProductPrototype,
    string ProductDisplayName,
    int Quantity,
    int TotalCost,
    string Status,
    string Requester,
    string? Reason,
    string Account) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
