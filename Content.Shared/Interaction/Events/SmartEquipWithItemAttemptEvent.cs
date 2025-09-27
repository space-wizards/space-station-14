namespace Content.Shared.Interaction.Events;

/// <summary>
///     Raised on an entity attempted to be smart-equipped.
/// </summary>
/// <param name="HeldItem">The entity that is held in the active hand. Null, if nothing is held.</param>
[ByRefEvent]
public record struct SmartEquipWithItemAttemptEvent(EntityUid user, EntityUid HeldItem, EntityUid SlotEntity);
