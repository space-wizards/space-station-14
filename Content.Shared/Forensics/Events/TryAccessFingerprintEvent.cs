using Content.Shared.Inventory;

namespace Content.Shared.Forensics.Events;

/// <summary>
/// An event to check if the fingerprint is accessible.
/// </summary>
[ByRefEvent]
public record struct TryAccessFingerprintEvent : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;

    /// <summary>
    ///     Entity that blocked access.
    /// </summary>
    public EntityUid? Blocker;

    /// <summary>
    /// The fingerprint string from the entity, if they have fingerprints.
    /// </summary>
    public string? Fingerprint;
}
