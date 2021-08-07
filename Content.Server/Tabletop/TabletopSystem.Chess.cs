using Robust.Shared.Map;

namespace Content.Server.Tabletop
{
    public partial class TabletopSystem
    {
        private void SetupChessBoard(MapId mapId)
        {
            var chessboard = EntityManager.SpawnEntity("ChessBoardTabletop", new MapCoordinates(-1, 0, mapId));
            chessboard.Transform.Anchored = true;

            SpawnPieces(new MapCoordinates(-4.5f, 3.5f, mapId));
        }

        private void SpawnPieces(MapCoordinates topLeft, float separation = 1f)
        {
            var (mapId, x, y) = topLeft;

            // Spawn all black pieces
            SpawnPiecesRow("Black", topLeft, separation);
            SpawnPawns("Black", new MapCoordinates(x, y - separation, mapId) , separation);

            // Spawn all white pieces
            SpawnPawns("White", new MapCoordinates(x, y - 6 * separation, mapId) , separation);
            SpawnPiecesRow("White", new MapCoordinates(x, y - 7 * separation, mapId), separation);

            // Extra queens
            EntityManager.SpawnEntity("BlackQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            EntityManager.SpawnEntity("WhiteQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - 4 * separation, mapId));
        }

        // TODO: refactor to load FEN instead
        private void SpawnPiecesRow(string color, MapCoordinates left, float separation = 1f)
        {
            const string piecesRow = "rnbqkbnr";

            var (mapId, x, y) = left;

            for (int i = 0; i < 8; i++)
            {
                switch (piecesRow[i])
                {
                    case 'r':
                        EntityManager.SpawnEntity(color + "Rook", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'n':
                        EntityManager.SpawnEntity(color + "Knight", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'b':
                        EntityManager.SpawnEntity(color + "Bishop", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'q':
                        EntityManager.SpawnEntity(color + "Queen", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'k':
                        EntityManager.SpawnEntity(color + "King", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                }
            }
        }

        // TODO: refactor to load FEN instead
        private void SpawnPawns(string color, MapCoordinates left, float separation = 1f)
        {
            var (mapId, x, y) = left;

            for (int i = 0; i < 8; i++)
            {
                EntityManager.SpawnEntity(color + "Pawn", new MapCoordinates(x + i * separation, y, mapId));
            }
        }
    }
}
