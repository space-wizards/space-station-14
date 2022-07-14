using Content.Shared.Inventory;
using Content.Shared.Sound;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This handles entities which can be equipped.
/// </summary>
[RegisterComponent, NetworkedComponent]
public abstract class NewSharedClothingComponent : Component
{
    [DataField("clothingVisuals")]
    public Dictionary<string, List<SharedSpriteComponent.PrototypeLayerData>> ClothingVisuals = new();

    [DataField("quickEquip")]
    public bool QuickEquip = true;

    [ViewVariables]
    [DataField("slots", required: true)]
    public SlotFlags Slots = SlotFlags.NONE;

    [DataField("equipSound")]
    public SoundSpecifier? EquipSound = default!;

    [DataField("unequipSound")]
    public SoundSpecifier? UnequipSound = default!;
}
