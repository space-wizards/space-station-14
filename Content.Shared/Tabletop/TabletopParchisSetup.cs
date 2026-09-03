using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up a board game to play Parchis.
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopParchisSetup : TabletopSetup
{
    /// <summary>
    /// The entity prototype for the red player's pawns.
    /// </summary>
    [DataField]
    public EntProtoId RedPiecePrototype = "RedTabletopPiece";

    /// <summary>
    /// The entity prototype for the green player's pawns.
    /// </summary>
    [DataField]
    public EntProtoId GreenPiecePrototype = "GreenTabletopPiece";

    /// <summary>
    /// The entity prototype for the yellow player's pawns.
    /// </summary>
    [DataField]
    public EntProtoId YellowPiecePrototype = "YellowTabletopPiece";

    /// <summary>
    /// The entity prototype for the blue player's pawns.
    /// </summary>
    [DataField]
    public EntProtoId BluePiecePrototype = "BlueTabletopPiece";

    /// <inheritdoc />
    public override void SetupPieces(Entity<TabletopGameComponent> tabletop, EntityUid board, EntityManager entityManager)
    {
        // Outer and inner X coordinates for pieces.
        const float x1 = 6.25f;
        const float x2 = 4.25f;

        // Outer and inner Y coordinates for pieces.
        const float y1 = 6.25f;
        const float y2 = 4.25f;

        // Red pieces.
        SpawnPiece(RedPiecePrototype, new(-x1, -y1), board, entityManager);
        SpawnPiece(RedPiecePrototype, new(-x1, -y2), board, entityManager);
        SpawnPiece(RedPiecePrototype, new(-x2, -y1), board, entityManager);
        SpawnPiece(RedPiecePrototype, new(-x2, -y2), board, entityManager);

        // Green pieces.
        SpawnPiece(GreenPiecePrototype, new(x1, -y1), board, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x1, -y2), board, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x2, -y1), board, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x2, -y2), board, entityManager);

        // Yellow pieces.
        SpawnPiece(GreenPiecePrototype, new(x1, y1), board, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x1, y2), board, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x2, y1), board, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x2, y2), board, entityManager);

        // Blue pieces.
        SpawnPiece(BluePiecePrototype, new(-x1, y1), board, entityManager);
        SpawnPiece(BluePiecePrototype, new(-x1, y2), board, entityManager);
        SpawnPiece(BluePiecePrototype, new(-x2, y1), board, entityManager);
        SpawnPiece(BluePiecePrototype, new(-x2, y2), board, entityManager);
    }
}
