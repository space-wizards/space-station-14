using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// Indicates that this meleeweapon requires a certain object in a slot to be useable.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ItemSlotsComponent))]
public sealed partial class MeleeRequiresObjectInSlotComponent : Component
{
    [DataField]
    public string SlotName = string.Empty;
}
