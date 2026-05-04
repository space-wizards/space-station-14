namespace Content.Shared.Inventory.Events;

public abstract class UnequippedEventBase(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition) : EntityEventArgs
{
    /// <summary>
    /// The entity which was unequipped from.
    /// NOT necessarily the one who performed the interaction.
    /// </summary>
    public readonly EntityUid EquipTarget = equipTarget;

    /// <summary>
    /// The entity which got unequipped.
    /// </summary>
    public readonly EntityUid Equipment = equipment;

    /// <summary>
    /// The slot the entity got unequipped from.
    /// </summary>
    public readonly string Slot = slotDefinition.Name;

    /// <summary>
    /// The slot group the entity got unequipped from.
    /// </summary>
    public readonly string SlotGroup = slotDefinition.SlotGroup;

    /// <summary>
    /// Slotflags of the slot the entity just got unequipped from.
    /// </summary>
    public readonly SlotFlags SlotFlags = slotDefinition.SlotFlags;
}

/// <summary>
/// Raised directed on an equipee when something was unequipped.
/// </summary>
public sealed class DidUnequipEvent(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition)
    : UnequippedEventBase(equipTarget, equipment, slotDefinition);

/// <summary>
/// Raised directed on equipment when it was unequipped from an equipee.
/// </summary>
public sealed class GotUnequippedEvent(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition)
    : UnequippedEventBase(equipTarget, equipment, slotDefinition);
