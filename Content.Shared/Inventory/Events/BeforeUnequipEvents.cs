namespace Content.Shared.Inventory.Events;

public abstract class BeforeUnequipEventBase(EntityUid user, EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition) : EntityEventArgs
{
    /// <summary>
    /// The entity performing the action.
    /// NOT necessarily the same as the entity whose equipment is being removed.
    /// </summary>
    public readonly EntityUid User = user;

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
/// Raised directed on an equipee before something is unequipped from them.
/// Note: This is only raised when enequipped using TryUnequip, not if the container is directly modified or the item is deleted.
/// </summary>
public sealed class BeforeUnequipEvent(EntityUid user, EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition)
    : BeforeUnequipEventBase(user, equipTarget, equipment, slotDefinition);

/// <summary>
/// Raised directed on equipment before it is unequipped from an equipee.
/// Note: This is only raised when enequipped using TryUnequip, not if the container is directly modified or the item is deleted.
/// </summary>
public sealed class BeforeGettingUnequippedEvent(EntityUid user, EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition)
    : BeforeUnequipEventBase(user, equipTarget, equipment, slotDefinition);
