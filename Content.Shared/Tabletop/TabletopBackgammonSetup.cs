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
    [DataField]
    public EntProtoId WhitePiecePrototype = "WhiteTabletopPiece";

    [DataField]
    public EntProtoId BlackPiecePrototype = "BlackTabletopPiece";

    /// <summary>
    /// Sets up a game of backgammon at the coordinates given.
    /// </summary>
    public override void SetupTabletop(TabletopGameComponent tabletop, MapCoordinates coordinates, IEntityManager entityManager)
    {
        tabletop.Board = entityManager.Spawn(BoardPrototype, coordinates);

        const float borderLengthX = 7.35f; //BORDER
        const float borderLengthY = 5.60f; //BORDER

        const float boardDistanceX = 1.25f;
        const float pieceDistanceY = 0.80f;

        float GetXPosition(float distanceFromSide, bool isLeftSide)
        {
            var pos = borderLengthX - distanceFromSide * boardDistanceX;
            return isLeftSide ? -pos : pos;
        }

        float GetYPosition(float positionNumber, bool isTop)
        {
            var pos = borderLengthY - pieceDistanceY * positionNumber;
            return isTop ? pos : -pos;
        }

        void AddPieces(
            float distanceFromSide,
            int numberOfPieces,
            bool isBlackPiece,
            bool isTop,
            bool isLeftSide)
        {
            var pieceProtoId = isBlackPiece ? BlackPiecePrototype : WhitePiecePrototype;
            for (var i = 0; i < numberOfPieces; i++)
                SpawnPiece(pieceProtoId, new(GetXPosition(distanceFromSide, isLeftSide), GetYPosition(i, isTop)), tabletop, entityManager);
        }

        // top left
        AddPieces(0, 5, true, true, true);
        // top middle left
        AddPieces(4, 3, false, true, true);
        // top middle right
        AddPieces(5, 5, false, true, false);
        // top far right
        AddPieces(0, 2, true, true, false);
        // bottom left
        AddPieces(0, 5, false, false, true);
        // bottom middle left
        AddPieces(4, 3, true, false, true);
        // bottom middle right
        AddPieces(5, 5, true, false, false);
        // bottom far right
        AddPieces(0, 2, false, false, false);
    }
}
