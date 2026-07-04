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
    /// Each group that makes up this category.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<PaintableGroupPrototype>> Groups = new();
}
