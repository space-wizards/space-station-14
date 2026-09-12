using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.StatusIcon.Components;

/// <summary>
/// Entities with this component grant the StatusIconComponent to entities that equip or hold this entity (as applicable).
/// StatusIconComponent is only granted as long as any entity with this component is held or equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(StatusIconEquipmentSystem))]
public sealed partial class StatusIconEquipmentComponent : Component
{
    /// <summary>
    /// If StatusIconComponent should be granted to an entity holding this item.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool IncludeHands = true;

    /// <summary>
    /// StatusIconComponent will be granted to any entity equipping this entity in a flagged slot.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public SlotFlags Slots = SlotFlags.All;
}
