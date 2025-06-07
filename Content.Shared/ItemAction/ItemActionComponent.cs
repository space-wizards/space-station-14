using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ItemAction;

/// <summary>
/// Component for the SummonItem action.
/// Used for storing an item within an action and summoning it into your hand on use.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(SharedItemActionSystem))]
public sealed partial class ItemActionComponent : Component
{
    /// <summary>
    /// Should the summoned item be unremovable?
    /// Used for when you don't want it to be dropped (such as spells, armblade, etc.)
    /// Items that are not unremovable cannot be deposited back into the action, so a new one will be spawned every time it is used.
    /// </summary>
    [DataField]
    public bool Unremovable = true;

    /// <summary>
    /// Whether the action has a currently summoned item.
    /// Does nothing if Unremovable is false.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Summoned;

    /// <summary>
    /// The item that will appear be spawned by the action.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnedPrototype;

    /// <summary>
    /// The item managed by the action. Will be summoned and hidden as the action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionItemUid;

    /// <summary>
    /// The item slot used to store the item.
    /// </summary>
    [DataField]
    public ItemSlot ItemSlot = new ();

    public const string ItemSlotId = "item-action-item-slot";
}
