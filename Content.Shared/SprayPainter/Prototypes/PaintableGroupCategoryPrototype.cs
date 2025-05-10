using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter.Prototypes;

/// <summary>
/// Used to display an individual tab in the spray painter.
/// </summary>
[Prototype]
public sealed partial class PaintableGroupCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The value of this field is subtracted from the total value of the paint sprayer charges,
    /// i.e. it is how much charge the paint sprayer will spend for painting an object from this category.
    /// </summary>
    [DataField]
    public int Cost = 1;
}
