using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter.Prototypes;

/// <summary>
/// A category of spray paintable items (e.g. airlocks, crates)
/// </summary>
[Prototype]
public sealed partial class PaintableGroupCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// To number of charges needed to paint an object of this category.
    /// </summary>
    [DataField]
    public int Cost = 1;
}
