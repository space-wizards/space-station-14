using Content.Shared.Inventory;

namespace Content.Shared.Flash;

/// <summary>
/// Called before a flash is used to check if the attempt is cancelled by blindness, items or FlashImmunityComponent.
/// Raised on the target hit by the flash and their inventory items.
/// </summary>
[ByRefEvent]
public record struct FlashAttemptEvent(EntityUid Target, EntityUid? User, EntityUid? Used, bool Cancelled = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.HEAD | SlotFlags.EYES | SlotFlags.MASK;
}

/// <summary>
/// Called when a player is successfully flashed.
/// Raised on the target hit by the flash, the user of the flash and the flash used.
/// The Melee parameter is used to check for rev conversion.
/// </summary>
[ByRefEvent]
public record struct AfterFlashedEvent(EntityUid Target, EntityUid? User, EntityUid? Used, bool Melee);
