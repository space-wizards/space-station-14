using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This handles entities which can be equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ClothingSystem), typeof(InventorySystem))]
public sealed partial class ClothingComponent : Component
{
    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

    /// <summary>
    /// The name of the layer in the user that this piece of clothing will map to
    /// </summary>
    [DataField]
    public string? MappedLayer;

    [DataField]
    public bool QuickEquip = true;

    /// <summary>
    /// The slots in which the clothing is considered "worn" or "equipped". E.g., putting shoes in your pockets does not
    /// equip them as far as clothing related events are concerned.
    /// </summary>
    /// <remarks>
    /// Note that this may be a combination of different slot flags, not a singular bit.
    /// </remarks>
    [DataField(required: true)]
    [Access(typeof(ClothingSystem), typeof(InventorySystem), Other = AccessPermissions.ReadExecute)]
    public SlotFlags Slots = SlotFlags.NONE;

    [DataField]
    public SoundSpecifier? EquipSound;

    [DataField]
    public SoundSpecifier? UnequipSound;

    [Access(typeof(ClothingSystem))]
    [DataField, AutoNetworkedField]
    public string? EquippedPrefix;

    /// <summary>
    /// Allows the equipped state to be directly overwritten.
    /// useful when prototyping INNERCLOTHING items into OUTERCLOTHING items without duplicating/modifying RSIs etc.
    /// </summary>
    [Access(typeof(ClothingSystem))]
    [DataField, AutoNetworkedField]
    public string? EquippedState;

    [DataField("sprite")]
    public string? RsiPath;

    /// <summary>
    /// Name of the inventory slot the clothing is currently in.
    /// Note that this being non-null does not mean the clothing is considered "worn" or "equipped" unless the slot
    /// satisfies the <see cref="Slots"/> flags.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? InSlot;
    // TODO CLOTHING
    // Maybe keep this null unless its in a valid slot?
    // To lazy to figure out ATM if that would break anything.
    // And when doing this, combine InSlot and InSlotFlag, as it'd be a breaking change for downstreams anyway

    /// <summary>
    /// Slot flags of the slot the clothing is currently in. See also <see cref="InSlot"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags? InSlotFlag;
    // TODO CLOTHING
    // Maybe keep this null unless its in a valid slot?
    // And when doing this, combine InSlot and InSlotFlag, as it'd be a breaking change for downstreams anyway

    [DataField]
    public TimeSpan EquipDelay = TimeSpan.Zero;

    [DataField]
    public TimeSpan UnequipDelay = TimeSpan.Zero;

    /// <summary>
    /// Only works when EquipDelay or UnequipDelay > 0.
    /// Prevents clothing from being put on or taken off while moving.
    /// Some clothing can logically be put on while running (hats),
    /// while other types of clothing may require stopping (shoes, hard suits).
    /// </summary>
    [DataField]
    public bool EquipWhileMoving = false;

    /// <summary>
    /// Offset for the strip time for an entity with this component.
    /// Only applied when it is being equipped or removed by another player.
    /// </summary>
    [DataField]
    public TimeSpan StripDelay = TimeSpan.Zero;

    /// <summary>
    ///     A scale applied to all layers.
    /// </summary>
    [DataField]
    public Vector2 Scale = Vector2.One;
}

public enum ClothingMask : byte
{
    NoMask = 0,
    UniformFull,
    UniformTop
}

[Serializable, NetSerializable]
public sealed partial class ClothingEquipDoAfterEvent : DoAfterEvent
{
    public string Slot;

    public ClothingEquipDoAfterEvent(string slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class ClothingUnequipDoAfterEvent : DoAfterEvent
{
    public string Slot;

    public ClothingUnequipDoAfterEvent(string slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}
