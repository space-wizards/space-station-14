using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
    public ItemSize Size = ItemSize.Small;

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
}

[Serializable, NetSerializable]
public sealed class ItemComponentState : ComponentState
{
    public ItemSize Size { get; }
    public string? HeldPrefix { get; }

    public ItemComponentState(ItemSize size, string? heldPrefix)
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

/// <summary>
/// Abstracted sizes for items.
/// Used to determine what can fit into inventories.
/// </summary>
public enum ItemSize
{
    /// <summary>
    /// Items that can be held completely in one's hand.
    /// </summary>
    Tiny = 1,

    /// <summary>
    /// Items that can fit inside of a standard pocket.
    /// </summary>
    Small = 2,

    /// <summary>
    /// Items that can fit inside of a standard bag.
    /// </summary>
    Normal = 4,

    /// <summary>
    /// Items that are too large to fit inside of standard bags, but can worn in exterior slots or placed in custom containers.
    /// </summary>
    Large = 8,

    /// <summary>
    /// Items that are too large to place inside of any kind of container.
    /// </summary>
    Huge = 16,

    /// <summary>
    /// Picture furry gf
    /// </summary>
    Ginormous = 32
}
