using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for a <see cref="BorgModuleComponent"/> that provides items to the entity it's installed into.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBorgSystem))]
public sealed partial class ItemBorgModuleComponent : Component
{
    /// <summary>
    /// The hands that are provided.
    /// </summary>
    [DataField(required: true)]
    public List<BorgHand> Hands = new();

    /// <summary>
    /// The items stored within the hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> StoredItems = new();

    /// <summary>
    /// Whether the provided items have been spawned.
    /// This happens the first time the module is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Spawned;

    /// <summary>
    /// An ID for the container where items are stored when not in use.
    /// </summary>
    [DataField]
    public string HoldingContainer = "holding_container";
}

/// <summary>
/// A single hand provided by the module.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct BorgHand
{
    /// <summary>
    /// The item to spawn in the hand, if any.
    /// </summary>
    [DataField]
    public EntProtoId? Item;

    /// <summary>
    /// The settings for the hand, including a whitelist.
    /// </summary>
    [DataField]
    public Hand Hand = new();

    [DataField]
    public bool ForceRemovable = false;

    public BorgHand(EntProtoId? item, Hand hand, bool forceRemovable = false)
    {
        Item = item;
        Hand = hand;
        ForceRemovable = forceRemovable;
    }
}
