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
    /// The entity to spawn in the desired slots.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Item;

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
    /// The slots to put the provided item into.
    /// Will fill all the applicable slots.
    /// </summary>
    [DataField]
    public SlotFlags Slots = SlotFlags.NONE;

    /// <summary>
    /// Whether the item should be put in hands.
    /// </summary>
    [DataField]
    public bool Hands = true;

    /// <summary>
    /// If the item in the existing slots/hands should be dropped to make space.
    /// </summary>
    [DataField]
    public bool DropExisting = true;

    /// <summary>
    /// Forces the items to spawn in their respective spots.
    /// If trying to equip to a slot with an unremovable item, it will be deleted if this is true.
    /// Only affects inventory slots.
    /// </summary>
    [DataField]
    public bool Force;

    /// <summary>
    /// Whether at least one item was successfully spawned by the effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SuccessfullySpawned;

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
