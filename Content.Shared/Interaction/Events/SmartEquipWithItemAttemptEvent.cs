namespace Content.Shared.Interaction.Events;

/// <summary>
///     Raised on an entity in the respective slot of the smart-equip action (E.g. Belt for Shift + E).
///     If no entity is in said slot, this event isn't raised.
/// </summary>
/// <param name="HeldItem">The entity that is held in the active hand. Null, if nothing is held.</param>
/// <param name="User">The entity that tries to smart-equip the held item.</param>
[ByRefEvent]
public record struct SmartEquipWithItemAttemptEvent(EntityUid User, EntityUid HeldItem);
