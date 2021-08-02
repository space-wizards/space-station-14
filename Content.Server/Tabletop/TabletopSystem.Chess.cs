using Content.Shared.DrawDepth;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Tabletop
{
    public partial class TabletopSystem
    {
        private void SetupChessBoard(MapId mapId)
        {
            var chessboard = _entityManager.SpawnEntity("ChessBoard", new MapCoordinates(0, 0, mapId));

            if (chessboard.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                spriteComponent.Scale = new Vector2(16, 16);
                spriteComponent.DrawDepth = (int) DrawDepth.FloorTiles;
            }

            chessboard.Transform.Anchored = true;

            SpawnPieces(new MapCoordinates(-3.5f, 3.5f, mapId));
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
        }

        private void SpawnPiecesRow(string color, MapCoordinates left, float separation = 1f)
        {
            const string piecesRow = "rnbqkbnr";

            var (mapId, x, y) = left;

            for (int i = 0; i < 8; i++)
            {
                switch (piecesRow[i])
                {
                    case 'r':
                        _entityManager.SpawnEntity(color + "Rook", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'n':
                        _entityManager.SpawnEntity(color + "Knight", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'b':
                        _entityManager.SpawnEntity(color + "Bishop", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'q':
                        _entityManager.SpawnEntity(color + "Queen", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'k':
                        _entityManager.SpawnEntity(color + "King", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                }
            }
        }

        private void SpawnPawns(string color, MapCoordinates left, float separation = 1f)
        {
            var (mapId, x, y) = left;

            for (int i = 0; i < 8; i++)
            {
                _entityManager.SpawnEntity(color + "Pawn", new MapCoordinates(x + i * separation, y, mapId));
            }
        }
    }
}
