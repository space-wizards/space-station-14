using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Sound;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This handles entities which can be equipped.
/// </summary>
[NetworkedComponent]
public abstract class SharedClothingComponent : Component
{
    [DataField("clothingVisuals")]
    public Dictionary<string, List<SharedSpriteComponent.PrototypeLayerData>> ClothingVisuals = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("quickEquip")]
    public bool QuickEquip = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("slots", required: true)]
    public SlotFlags Slots = SlotFlags.NONE;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("equipSound")]
    public SoundSpecifier? EquipSound;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("unequipSound")]
    public SoundSpecifier? UnequipSound;

    [Access(typeof(ClothingSystem))]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("equippedPrefix")]
    public string? EquippedPrefix;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sprite")]
    public string? RsiPath;
}

[Serializable, NetSerializable]
public sealed class ClothingComponentState : ComponentState
{
    public string? EquippedPrefix;

    public ClothingComponentState(string? equippedPrefix)
    {
        EquippedPrefix = equippedPrefix;
    }
}
