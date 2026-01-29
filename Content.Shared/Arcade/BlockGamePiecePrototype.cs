using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade;

/// <summary>
///
/// </summary>
[Prototype]
public sealed partial class BlockGamePiecePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public Vector2i Dimensions;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public bool[] Piece = [];
}
