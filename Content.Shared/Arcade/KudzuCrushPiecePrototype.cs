using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade;

/// <summary>
///
/// </summary>
[Prototype]
public sealed partial class KudzuCrushPiecePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public Vector2i[] Cells = [];
}
