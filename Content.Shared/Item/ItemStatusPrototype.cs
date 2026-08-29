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
    /// Other item statuses that this status can be displayed alongside.
    /// If set, any status not in this list will be hidden when this status is present.
    /// An empty list means this status is shown by itself.
    /// </summary>
    [DataField]
    public List<ProtoId<ItemStatusPrototype>>? Whitelist;

    /// <summary>
    /// Other item statuses that this status cannot be displayed alongside.
    /// When multiple conflicting statuses are present, the one with higher priority wins.
    /// </summary>
    [DataField]
    public List<ProtoId<ItemStatusPrototype>>? Blacklist;
}
