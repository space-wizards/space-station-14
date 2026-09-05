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

    // The size of a square on the board, in meters.
    public const float PieceDistance = 1.0f;

    public override void SetupPieces(Entity<TabletopGameComponent> tabletop, EntityUid board, EntityManager entityManager)
    {
        var x = PieceOffsetX;
        var y = PieceOffsetY;
        var separation = PieceDistance;

        // Spawn all black pieces.
        SpawnPiecesRow(board, entityManager, "Black", new(x, y), separation);
        SpawnPawns(board, entityManager, "Black", new(x, y - separation), separation);

        // Spawn all white pieces.
        SpawnPawns(board, entityManager, "White", new(x, y - 6 * separation), separation);
        SpawnPiecesRow(board, entityManager, "White", new(x, y - 7 * separation), separation);

        // Extra queens.
        SpawnPiece("BlackQueen", new(x + 9 * separation + 5f / 32, y - 3 * separation), board, entityManager);
        SpawnPiece("WhiteQueen", new(x + 9 * separation + 5f / 32, y - 4 * separation), board, entityManager);
    }

    // TODO: refactor to load FEN instead
    private void SpawnPiecesRow(EntityUid board, EntityManager entityManager, string color, Vector2 left, float separation = 1f)
    {
        const string piecesRow = "rnbqkbnr";

        var (x, y) = left;

        for (var i = 0; i < 8; i++)
        {
            var coords = new Vector2(x + i * separation, y);
            switch (piecesRow[i])
            {
                case 'r':
                    SpawnPiece(color + "Rook", coords, board, entityManager);
                    break;
                case 'n':
                    SpawnPiece(color + "Knight", coords, board, entityManager);
                    break;
                case 'b':
                    SpawnPiece(color + "Bishop", coords, board, entityManager);
                    break;
                case 'q':
                    SpawnPiece(color + "Queen", coords, board, entityManager);
                    break;
                case 'k':
                    SpawnPiece(color + "King", coords, board, entityManager);
                    break;
            }
        }
    }

    // TODO: refactor to load FEN instead
    private void SpawnPawns(EntityUid board, EntityManager entityManager, string color, Vector2 left, float separation = 1f)
    {
        var (x, y) = left;

        EntProtoId pawnProtoId = color + "Pawn";

        for (var i = 0; i < 8; i++)
            SpawnPiece(pawnProtoId, new(x + i * separation, y), board, entityManager);
    }
}
