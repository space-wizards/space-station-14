using Robust.Shared.Prototypes;

namespace Content.Shared.Item;

/// <summary>
/// Defines how an item status is selected when an item has multiple statuses.
/// </summary>
[Prototype]
public sealed partial class ItemStatusPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Higher values are displayed above lower values and take precedence when multiple statuses conflict.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Defines which item statuses can be displayed alongside this status.
    /// An empty list means this status is displayed by itself.
    /// </summary>
    [DataField]
    public List<ProtoId<ItemStatusPrototype>>? Whitelist;

    /// <summary>
    /// Defines which item statuses cannot be displayed alongside this status.
    /// </summary>
    [DataField]
    public List<ProtoId<ItemStatusPrototype>>? Blacklist;
}
