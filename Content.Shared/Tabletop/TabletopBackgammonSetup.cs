using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up a board and pieces for a game of backgammon.
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopBackgammonSetup : TabletopSetup
{
    /// <summary>
    /// The entity prototype for the white player's tablemen.
    /// </summary>
    [DataField]
    public EntProtoId WhitePiecePrototype = "WhiteTabletopPiece";

    /// <summary>
    /// The entity prototype for the black player's tablemen.
    /// </summary>
    [DataField]
    public EntProtoId BlackPiecePrototype = "BlackTabletopPiece";

    // The distance from the center to the border of the board.
    const float BorderLengthX = 7.35f;
    const float BorderLengthY = 5.60f;

    // The distance between each point (triangle) on the board.
    const float BoardDistanceX = 1.25f;

    // The distance between each piece when placed on a triangle.
    const float PieceDistanceY = 0.80f;

    /// <summary>
    /// Sets up a game of backgammon at the coordinates given.
    /// </summary>
    public override void SetupPieces(Entity<TabletopGameComponent> tabletop, EntityUid board, EntityManager entityManager)
    {
        // top left
        AddPieces(0, 5, true, true, true, board, entityManager);
        // top middle left
        AddPieces(4, 3, false, true, true, board, entityManager);
        // top middle right
        AddPieces(5, 5, false, true, false, board, entityManager);
        // top far right
        AddPieces(0, 2, true, true, false, board, entityManager);
        // bottom left
        AddPieces(0, 5, false, false, true, board, entityManager);
        // bottom middle left
        AddPieces(4, 3, true, false, true, board, entityManager);
        // bottom middle right
        AddPieces(5, 5, true, false, false, board, entityManager);
        // bottom far right
        AddPieces(0, 2, false, false, false, board, entityManager);
    }

    float GetXPosition(float distanceFromSide, bool isLeftSide)
    {
        var pos = BorderLengthX - distanceFromSide * BoardDistanceX;
        return isLeftSide ? -pos : pos;
    }

    float GetYPosition(float positionNumber, bool isTop)
    {
        var pos = BorderLengthY - PieceDistanceY * positionNumber;
        return isTop ? pos : -pos;
    }

    void AddPieces(
        float distanceFromSide,
        int numberOfPieces,
        bool isBlackPiece,
        bool isTop,
        bool isLeftSide,
        EntityUid board,
        EntityManager entityManager)
    {
        var pieceProtoId = isBlackPiece ? BlackPiecePrototype : WhitePiecePrototype;
        for (var i = 0; i < numberOfPieces; i++)
            SpawnPiece(pieceProtoId, new(GetXPosition(distanceFromSide, isLeftSide), GetYPosition(i, isTop)), board, entityManager);
    }
}
