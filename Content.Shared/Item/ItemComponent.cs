using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Item;

/// <summary>
///     Handles items which can be picked up to hands and placed in pockets, as well as storage containers
///     like backpacks.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedItemSystem))]
public sealed partial class ItemComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(SharedItemSystem))]
    public ProtoId<ItemSizePrototype> Size = "Small";

    [Access(typeof(SharedItemSystem))]
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    [Access(typeof(SharedItemSystem))]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public string? HeldPrefix;

    /// <summary>
    ///     Rsi of the sprite shown on the player when this item is in their hands. Used to generate a default entry for <see cref="InhandVisuals"/>
    /// </summary>
    [Access(typeof(SharedItemSystem))]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sprite")]
    public string? RsiPath;

    /// <summary>
    /// An optional override for the shape of the item within the grid storage.
    /// If null, a default shape will be used based on <see cref="Size"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2i>? Shape;

    /// <summary>
    /// A sprite used to depict this entity specifically when it is displayed in the storage UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? StoredSprite;

    /// <summary>
    /// An additional angle offset, in degrees, applied to the visual depiction of the item when displayed in the storage UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StoredRotation = 0;
}

[Serializable, NetSerializable]
public sealed class ItemComponentState : ComponentState
{
    public ProtoId<ItemSizePrototype> Size { get; }
    public string? HeldPrefix { get; }

    public ItemComponentState(ProtoId<ItemSizePrototype> size, string? heldPrefix)
    {
        Size = size;
        HeldPrefix = heldPrefix;
    }
}

/// <summary>
///     Raised when an item's visual state is changed. The event is directed at the entity that contains this item, so
///     that it can properly update its hands or inventory sprites and GUI.
/// </summary>
[Serializable, NetSerializable]
public sealed class VisualsChangedEvent : EntityEventArgs
{
    public readonly NetEntity Item;
    public readonly string ContainerId;

    public VisualsChangedEvent(NetEntity item, string containerId)
    {
        Item = item;
        ContainerId = containerId;
    }
}
