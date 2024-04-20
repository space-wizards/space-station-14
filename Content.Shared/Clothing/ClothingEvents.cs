
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing;

/// <summary>
///     Raised directed at a piece of clothing to get the set of layers to show on the wearer's sprite
/// </summary>
public sealed class GetEquipmentVisualsEvent : EntityEventArgs
{
    /// <summary>
    ///     Entity that is wearing the item.
    /// </summary>
    public readonly EntityUid Equipee;

    public readonly string Slot;

    /// <summary>
    ///     The layers that will be added to the entity that is wearing this item.
    /// </summary>
    /// <remarks>
    ///     Note that the actual ordering of the layers depends on the order in which they are added to this list;
    /// </remarks>
    public List<(string, PrototypeLayerData)> Layers = new();

    public GetEquipmentVisualsEvent(EntityUid equipee, string slot)
    {
        Equipee = equipee;
        Slot = slot;
    }
}

/// <summary>
///     Raised directed at a piece of clothing after its visuals have been updated.
/// </summary>
/// <remarks>
///     Useful for systems/components that modify the visual layers that an item adds to a player. (e.g. RGB memes)
/// </remarks>
public sealed class EquipmentVisualsUpdatedEvent : EntityEventArgs
{
    /// <summary>
    ///     Entity that is wearing the item.
    /// </summary>
    public readonly EntityUid Equipee;

    public readonly string Slot;

    /// <summary>
    ///     The layers that this item is now revealing.
    /// </summary>
    public HashSet<string> RevealedLayers;

    public EquipmentVisualsUpdatedEvent(EntityUid equipee, string slot, HashSet<string> revealedLayers)
    {
        Equipee = equipee;
        Slot = slot;
        RevealedLayers = revealedLayers;
    }
}

public sealed partial class ToggleMaskEvent : InstantActionEvent { }

/// <summary>
///     Event raised on the mask entity when it is toggled.
/// </summary>
[ByRefEvent]
public readonly record struct ItemMaskToggledEvent(EntityUid Wearer, string? equippedPrefix, bool IsToggled, bool IsEquip);

/// <summary>
///     Event raised on the entity wearing the mask when it is toggled.
/// </summary>
[ByRefEvent]
public readonly record struct WearerMaskToggledEvent(bool IsToggled);

/// <summary>
/// Raised on the clothing entity when it is equipped to a valid slot,
/// as determined by <see cref="ClothingComponent.Slots"/>.
/// </summary>
[ByRefEvent]
public readonly record struct ClothingGotEquippedEvent(EntityUid Wearer, ClothingComponent Clothing);

/// <summary>
/// Raised on the clothing entity when it is unequipped from a valid slot,
/// as determined by <see cref="ClothingComponent.Slots"/>.
/// </summary>
[ByRefEvent]
public readonly record struct ClothingGotUnequippedEvent(EntityUid Wearer, ClothingComponent Clothing);

/// <summary>
/// Raised on an entity when they equip a clothing item to a valid slot,
/// as determined by <see cref="ClothingComponent.Slots"/>.
/// </summary>
[ByRefEvent]
public readonly record struct ClothingDidEquippedEvent(Entity<ClothingComponent> Clothing);

/// <summary>
/// Raised on an entity when they unequip a clothing item from a valid slot,
/// as determined by <see cref="ClothingComponent.Slots"/>.
/// </summary>
[ByRefEvent]
public readonly record struct ClothingDidUnequippedEvent(Entity<ClothingComponent> Clothing);
