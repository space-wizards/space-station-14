using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

/// <summary>
/// Defines a category of second-hand (worn/damaged) uplink items with a weighted chance to be selected.
/// </summary>
[Prototype]
[DataDefinition]
public sealed partial class SecondHandCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Weight that sets the chance to roll this category during second-hand item selection.
    /// </summary>
    [DataField]
    public int Weight { get; private set; }

    /// <summary>
    /// Maximum number of items that can be selected from this category.
    /// If null, there is no limit beyond the total item count.
    /// </summary>
    [DataField]
    public int? MaxItems { get; private set; }
}
