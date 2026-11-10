using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Item;

/// <summary>
/// Component used for status effects.
/// Forces an item onto the entity the status effect was applied to, either to their inventory or hands.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ForcedItemStatusEffectComponent : Component
{
    /// <summary>
    /// The entities to spawn in the hands of the user.
    /// If the hands are unavailable, they will not be spawned.
    /// </summary>
    [DataField]
    public List<EntProtoId> HandItems = new ();

    /// <summary>
    /// The entities to spawn in the inventory of the user.
    /// If the slots are unavailable, they will not be spawned.
    /// </summary>
    [DataField]
    public Dictionary<SlotFlags, EntProtoId> InventoryItems = new ();

    /// <summary>
    /// <see cref="EntityUid"/>s of the spawned items. Used to remove them when the status effect expires.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ItemEntities = new ();

    /// <summary>
    /// Whether the item should be unremovable from the slot it was placed in.
    /// </summary>
    [DataField]
    public bool Unremovable = true;

    /// <summary>
    /// If the item in the existing slots/hands should be dropped to make space.
    /// </summary>
    [DataField]
    public bool DropExisting = true;

    /// <summary>
    /// Forces the items to spawn in their respective slots, even if usually not possible to equip there.
    /// Only affects inventory slots.
    /// </summary>
    [DataField]
    public bool Force;

    /// <summary>
    /// Whether the status effect items should be removed when handcuffed.
    /// Causes the status effect to get removed as well.
    /// </summary>
    [DataField]
    public bool RemoveWhenCuffed = true;

    /// <summary>
    /// The sound to play when the item(s) spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SpawnSound;

    /// <summary>
    /// The sound to play when the item(s) disappear.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DespawnSound;
}
