using Robust.Shared.GameStates;

namespace Content.Shared.Inventory;

/// <summary>
/// This is used for an item that can only be equipped/unequipped by the user.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SelfEquipOnlySystem))]
public sealed partial class SelfEquipOnlyComponent : Component
{
    /// <summary>
    /// Whether or not the self-equip only condition requires the person to be conscious.
    /// </summary>
    [DataField]
    public bool UnequipRequireConscious = true;
}
