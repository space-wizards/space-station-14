namespace Content.Shared.Inventory.Events;

public abstract class BeforeEquipEventBase(EntityUid user, EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition) : EntityEventArgs
{
    /// <summary>
    /// The entity performing the action.
    /// NOT necessarily the one actually "receiving" the equipment.
    /// </summary>
    public readonly EntityUid User = user;

    /// <summary>
    /// The entity which got equipped to.
    /// NOT necessarily the one who performed the interaction.
    /// </summary>
    public readonly EntityUid EquipTarget = equipTarget;

    /// <summary>
    /// The entity which got equipped.
    /// </summary>
    public readonly EntityUid Equipment = equipment;

    /// <summary>
    /// The slot the entity got equipped to.
    /// </summary>
    public readonly string Slot = slotDefinition.Name;

    /// <summary>
    /// The slot group the entity got equipped in.
    /// </summary>
    public readonly string SlotGroup = slotDefinition.SlotGroup;

    /// <summary>
    /// Slotflags of the slot the entity just got equipped to.
    /// </summary>
    public readonly SlotFlags SlotFlags = slotDefinition.SlotFlags;
}

/// <summary>
/// Raised directed on an equipee before something is equipped to them.
/// Note: This is only raised when equipped using TryEquip, not if the container is directly modified.
/// </summary>
public sealed class BeforeEquipEvent(EntityUid user, EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition)
    : BeforeEquipEventBase(user, equipTarget, equipment, slotDefinition);

/// <summary>
/// Raised directed on equipment before it is equipped to an equipee.
/// Note: This is only raised when equipped using TryEquip, not if the container is directly modified.
/// </summary>
public sealed class BeforeGettingEquippedEvent(EntityUid user, EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition)
    : BeforeEquipEventBase(user, equipTarget, equipment, slotDefinition);
