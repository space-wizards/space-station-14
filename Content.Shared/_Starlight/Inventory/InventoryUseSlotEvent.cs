namespace Content.Shared._Starlight.Inventory;

/// <summary>
/// Raised when a lot slot is "used", this being unhandled will result in the default unequip action
/// </summary>
[ByRefEvent]
public record struct InventoryUseSlotEvent(EntityUid Actor, EntityUid Target, bool Handled = false);