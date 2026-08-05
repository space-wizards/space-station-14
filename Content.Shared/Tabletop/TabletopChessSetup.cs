using System.Numerics;
using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up pieces and a board for a game of chess.
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopChessSetup : TabletopSetup
{
    // TODO: Un-hardcode the rest of entity prototype IDs, probably.

    // The coordinates of the center of the left bottom corner square.
    public const float PieceOffsetX = -4.5f;
    public const float PieceOffsetY = 3.5f;

    /// <inheritdoc />
    public override void SetupTabletop(TabletopGameComponent tabletop, MapCoordinates coordinates, EntityManager entityManager)
    {
        tabletop.Board = entityManager.SpawnEntity(BoardPrototype, coordinates);

        SpawnPieces(tabletop, entityManager);
    }

    private void SpawnPieces(TabletopGameComponent tabletop, IEntityManager entityManager, float separation = 1f)
    {
        var x = PieceOffsetX;
        var y = PieceOffsetY;

        // Spawn all black pieces.
        SpawnPiecesRow(tabletop, entityManager, "Black", new(x, y), separation);
        SpawnPawns(tabletop, entityManager, "Black", new(x, y - separation), separation);

        // Spawn all white pieces.
        SpawnPawns(tabletop, entityManager, "White", new(x, y - 6 * separation), separation);
        SpawnPiecesRow(tabletop, entityManager, "White", new(x, y - 7 * separation), separation);

        // Extra queens.
        SpawnPiece("BlackQueen", new(x + 9 * separation + 5f / 32, y - 3 * separation), tabletop, entityManager);
        SpawnPiece("WhiteQueen", new(x + 9 * separation + 5f / 32, y - 4 * separation), tabletop, entityManager);
    }

    // TODO: refactor to load FEN instead
    private void SpawnPiecesRow(TabletopGameComponent tabletop, IEntityManager entityManager, string color, Vector2 left, float separation = 1f)
    {
        const string piecesRow = "rnbqkbnr";

        var (x, y) = left;

        for (var i = 0; i < 8; i++)
        {
            var coords = new Vector2(PieceOffsetX + x + i * separation, PieceOffsetY + y);
            switch (piecesRow[i])
            {
                case 'r':
                    SpawnPiece(color + "Rook", coords, tabletop, entityManager);
                    break;
                case 'n':
                    SpawnPiece(color + "Knight", coords, tabletop, entityManager);
                    break;
                case 'b':
                    SpawnPiece(color + "Bishop", coords, tabletop, entityManager);
                    break;
                case 'q':
                    SpawnPiece(color + "Queen", coords, tabletop, entityManager);
                    break;
                case 'k':
                    SpawnPiece(color + "King", coords, tabletop, entityManager);
                    break;
            }
        }
    }

    // TODO: refactor to load FEN instead
    private void SpawnPawns(TabletopGameComponent tabletop, IEntityManager entityManager, string color, Vector2 left, float separation = 1f)
    {
        var (x, y) = left;

        EntProtoId pawnProtoId = color + "Pawn";

        for (var i = 0; i < 8; i++)
            SpawnPiece(pawnProtoId, new(x + i * separation, y), tabletop, entityManager);
    }
}
