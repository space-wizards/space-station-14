using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;


/// <summary>
/// This component is added to entities to protect them from being damaged
/// when interacting with objects with the <see cref="DamageOnInteractComponent"/>
/// If the entity has sufficient protection, interaction with the object is not cancelled.
/// This allows the user to do things like remove a lightbulb.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageOnInteractProtectionComponent : Component, IClothingSlots
{
    /// <summary>
    /// How much and what kind of damage to protect the user from
    /// when interacting with something with <see cref="DamageOnInteractComponent"/>
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet DamageProtection = default!;

    /// <summary>
    /// Only protects if the item is in the correct slot
    /// i.e. having gloves in your pocket doesn't protect you, it has to be on your hands
    /// </summary>
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.GLOVES;
}
