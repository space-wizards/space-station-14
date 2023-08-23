using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Storage.Components;

/// <summary>
/// This is used for things like paper bins, in which
/// you can only take off of the top of the bin.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BinSystem))]
public sealed partial class BinComponent : Component
{
    /// <summary>
    /// The containers that contain the items held in the bin
    /// </summary>
    [ViewVariables]
    public Container ItemContainer = default!;

    /// <summary>
    /// A list representing the order in which
    /// all the entities are stored in the bin.
    /// </summary>
    /// <remarks>
    /// The only reason this isn't a stack is so that
    /// i can handle entities being deleted and removed
    /// out of order by other systems
    /// </remarks>
    [DataField("items")]
    public List<EntityUid> Items = new();

    /// <summary>
    /// The items that start in the bin. Sorted in order.
    /// </summary>
    [DataField("initialContents", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> InitialContents = new();

    /// <summary>
    /// A whitelist governing what items can be inserted into the bin.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The maximum amount of items
    /// that can be stored in the bin.
    /// </summary>
    [DataField("maxItems")]
    public int MaxItems = 20;
}

[Serializable, NetSerializable]
public sealed class BinComponentState : ComponentState
{
    public List<NetEntity> Items;

    public EntityWhitelist? Whitelist;

    public int MaxItems;

    public BinComponentState(List<NetEntity> items, EntityWhitelist? whitelist, int maxItems)
    {
        Items = items;
        Whitelist = whitelist;
        MaxItems = maxItems;
    }
}
