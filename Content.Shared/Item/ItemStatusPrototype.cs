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
    /// Higher values are displayed above lower values and take precedence when multiple statuses override others.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// If true, this status is shown by itself and hides all other statuses.
    /// </summary>
    [DataField]
    public bool Override;
}
