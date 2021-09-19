using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop
{
    public class TabletopChessSetup : TabletopSetup
    {
        [DataField("boardPrototype")]
        public string ChessBoardPrototype { get; } = "ChessBoardTabletop";

        // TODO: Un-hardcode the rest of entity prototype IDs, probably.

        public override void SetupTabletop(MapId mapId, IEntityManager entityManager)
        {
            var chessboard = entityManager.SpawnEntity(ChessBoardPrototype, new MapCoordinates(-1, 0, mapId));
            chessboard.Transform.Anchored = true;

            SpawnPieces(entityManager, new MapCoordinates(-4.5f, 3.5f, mapId));
        }

        private void SpawnPieces(IEntityManager entityManager, MapCoordinates topLeft, float separation = 1f)
        {
            var (mapId, x, y) = topLeft;

            // Spawn all black pieces
            SpawnPiecesRow(entityManager, "Black", topLeft, separation);
            SpawnPawns(entityManager, "Black", new MapCoordinates(x, y - separation, mapId) , separation);

            // Spawn all white pieces
            SpawnPawns(entityManager, "White", new MapCoordinates(x, y - 6 * separation, mapId) , separation);
            SpawnPiecesRow(entityManager, "White", new MapCoordinates(x, y - 7 * separation, mapId), separation);

            // Extra queens
            entityManager.SpawnEntity("BlackQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            entityManager.SpawnEntity("WhiteQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - 4 * separation, mapId));
        }

        // TODO: refactor to load FEN instead
        private void SpawnPiecesRow(IEntityManager entityManager, string color, MapCoordinates left, float separation = 1f)
        {
            const string piecesRow = "rnbqkbnr";

            var (mapId, x, y) = left;

            for (int i = 0; i < 8; i++)
            {
                switch (piecesRow[i])
                {
                    case 'r':
                        entityManager.SpawnEntity(color + "Rook", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'n':
                        entityManager.SpawnEntity(color + "Knight", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'b':
                        entityManager.SpawnEntity(color + "Bishop", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'q':
                        entityManager.SpawnEntity(color + "Queen", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'k':
                        entityManager.SpawnEntity(color + "King", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                }
            }
        }

        // TODO: refactor to load FEN instead
        private void SpawnPawns(IEntityManager entityManager, string color, MapCoordinates left, float separation = 1f)
        {
            var (mapId, x, y) = left;

            for (int i = 0; i < 8; i++)
            {
                entityManager.SpawnEntity(color + "Pawn", new MapCoordinates(x + i * separation, y, mapId));
            }
        }
    }
}
