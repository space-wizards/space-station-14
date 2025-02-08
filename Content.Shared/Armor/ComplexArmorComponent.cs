using Content.Shared.Damage;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

/// <summary>
///     Used for clothing that reduces damage when worn with specific clothing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedArmorSystem))]
public sealed partial class ComplexArmorComponent : Component, IClothingSlots
{
    /// <summary>
    ///     The damage reduction
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageModifierSet Modifiers = default!;

    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.HEAD;

    [DataField(required: true), AutoNetworkedField]
    public String ClothingTag = default!;
}
