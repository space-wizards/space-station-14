using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopChessSetup : TabletopSetup
{
    // TODO: Un-hardcode the rest of entity prototype IDs, probably.

    public override void SetupTabletop(TabletopGameComponent tabletop, IEntityManager entityManager)
    {
        var chessboard = entityManager.SpawnEntity(BoardPrototype, tabletop.Position);

        tabletop.Entities.Add(chessboard);

        SpawnPieces(tabletop, entityManager, tabletop.Position.Offset(-4.5f, 3.5f));
    }

    private void SpawnPieces(TabletopGameComponent tabletop, IEntityManager entityManager, MapCoordinates topLeft, float separation = 1f)
    {
        var (mapId, x, y) = topLeft;

        // Spawn all black pieces.
        SpawnPiecesRow(tabletop, entityManager, "Black", topLeft, separation);
        SpawnPawns(tabletop, entityManager, "Black", new MapCoordinates(x, y - separation, mapId), separation);

        // Spawn all white pieces.
        SpawnPawns(tabletop, entityManager, "White", new MapCoordinates(x, y - 6 * separation, mapId), separation);
        SpawnPiecesRow(tabletop, entityManager, "White", new MapCoordinates(x, y - 7 * separation, mapId), separation);

        // Extra queens.
        SpawnPiece("BlackQueen", new MapCoordinates(x + 9 * separation + 5f / 32, y - 3 * separation, mapId), tabletop, entityManager);
        SpawnPiece("WhiteQueen", new MapCoordinates(x + 9 * separation + 5f / 32, y - 4 * separation, mapId), tabletop, entityManager);
    }

    // TODO: refactor to load FEN instead
    private void SpawnPiecesRow(TabletopGameComponent tabletop, IEntityManager entityManager, string color, MapCoordinates left, float separation = 1f)
    {
        const string piecesRow = "rnbqkbnr";

        var (mapId, x, y) = left;

        for (var i = 0; i < 8; i++)
        {
            var coords = new MapCoordinates(x + i * separation, y, mapId);
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
    private void SpawnPawns(TabletopGameComponent tabletop, IEntityManager entityManager, string color, MapCoordinates left, float separation = 1f)
    {
        var (mapId, x, y) = left;

        EntProtoId pawnProtoId = color + "Pawn";

        for (var i = 0; i < 8; i++)
            SpawnPiece(pawnProtoId, new MapCoordinates(x + i * separation, y, mapId), tabletop, entityManager);
    }
}
