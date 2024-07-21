using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;


/// <summary>
/// This component is added to entities to protect them from being damaged
/// when attacking objects with the <see cref="DamageOnAttackedComponent"/>
/// If the entity has sufficient protection, the entity will take no damage.
/// </summary>

[RegisterComponent, NetworkedComponent]
public sealed partial class DamageOnAttackedProtectionComponent : Component, IClothingSlots
{
    /// <summary>
    /// How much and what kind of damage to protect the user from
    /// when interacting with something with <see cref="DamageOnInteractComponent"/>
    /// </summary>
    [DataField]
    public DamageSpecifier DamageProtection = default!;

    /// <summary>
    /// Only protects if the item is in the correct slot
    /// i.e. having gloves in your pocket doesn't protect you, it has to be on your hands
    /// </summary>
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.WITHOUT_POCKET;
}
