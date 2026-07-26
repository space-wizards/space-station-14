namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for item movement events: pickup, drop, throw, storage insert/remove, and stripping.
/// Covers <c>LogType.Pickup</c>, <c>LogType.Drop</c>, <c>LogType.Throw</c>,
/// <c>LogType.Landed</c>, <c>LogType.Storage</c>, and <c>LogType.Stripping</c>.
/// </summary>
/// <remarks>
/// The actor and item entity are captured as participants.
/// </remarks>
/// <param name="ItemPrototype">Item prototype ID.</param>
/// <param name="ItemDisplayName">Snapshot display name for historical context.</param>
/// <param name="Quantity">Stack quantity for stackable items. Null for unstackable items.</param>
/// <param name="SourceContainerPrototype">Prototype of the source container, or null if picked from the world/hands.</param>
/// <param name="DestinationContainerPrototype">Prototype of the destination container, or null if dropped to the world/hands.</param>
/// <param name="SlotName">Inventory slot or hand name for stripping events. Null for non-slot transfers.</param>
public sealed record ItemTransferLogPayload(
    string? ItemPrototype,
    string? ItemDisplayName,
    int? Quantity = null,
    string? SourceContainerPrototype = null,
    string? DestinationContainerPrototype = null,
    string? SlotName = null
) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
