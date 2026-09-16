namespace Content.Shared.Inventory.Events;

public abstract class EquippedEventBase(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition) : EntityEventArgs
{
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
/// Raised directed on an equipee when something was equipped.
/// </summary>
public sealed class DidEquipEvent(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition)
    : EquippedEventBase(equipTarget, equipment, slotDefinition);

/// <summary>
/// Raised directed on equipment when it was equipped to an equipee.
/// </summary>
public sealed class GotEquippedEvent(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition)
    : EquippedEventBase(equipTarget, equipment, slotDefinition);
