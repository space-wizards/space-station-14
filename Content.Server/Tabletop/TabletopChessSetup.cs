using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public class TabletopChessSetup : TabletopSetup
    {
        [DataField("boardPrototype")]
        public string ChessBoardPrototype { get; } = "ChessBoardTabletop";

        // TODO: Un-hardcode the rest of entity prototype IDs, probably.

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var chessboard = entityManager.SpawnEntity(ChessBoardPrototype, session.Position.Offset(-1, 0));

            session.Entities.Add(chessboard.Uid);

            SpawnPieces(session, entityManager, session.Position.Offset(-4.5f, 3.5f));
        }

        private void SpawnPieces(TabletopSession session, IEntityManager entityManager, MapCoordinates topLeft, float separation = 1f)
        {
            var (mapId, x, y) = topLeft;

            // Spawn all black pieces
            SpawnPiecesRow(session, entityManager, "Black", topLeft, separation);
            SpawnPawns(session, entityManager, "Black", new MapCoordinates(x, y - separation, mapId) , separation);

            // Spawn all white pieces
            SpawnPawns(session, entityManager, "White", new MapCoordinates(x, y - 6 * separation, mapId) , separation);
            SpawnPiecesRow(session, entityManager, "White", new MapCoordinates(x, y - 7 * separation, mapId), separation);

            // Extra queens
            session.Entities.Add(entityManager.SpawnEntity("BlackQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId)).Uid);
            session.Entities.Add(entityManager.SpawnEntity("WhiteQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - 4 * separation, mapId)).Uid);
        }

        // TODO: refactor to load FEN instead
        private void SpawnPiecesRow(TabletopSession session, IEntityManager entityManager, string color, MapCoordinates left, float separation = 1f)
        {
            const string piecesRow = "rnbqkbnr";

            var (mapId, x, y) = left;

            for (int i = 0; i < 8; i++)
            {
                switch (piecesRow[i])
                {
                    case 'r':
                        session.Entities.Add(entityManager.SpawnEntity(color + "Rook", new MapCoordinates(x + i * separation, y, mapId)).Uid);
                        break;
                    case 'n':
                        session.Entities.Add(entityManager.SpawnEntity(color + "Knight", new MapCoordinates(x + i * separation, y, mapId)).Uid);
                        break;
                    case 'b':
                        session.Entities.Add(entityManager.SpawnEntity(color + "Bishop", new MapCoordinates(x + i * separation, y, mapId)).Uid);
                        break;
                    case 'q':
                        session.Entities.Add(entityManager.SpawnEntity(color + "Queen", new MapCoordinates(x + i * separation, y, mapId)).Uid);
                        break;
                    case 'k':
                        session.Entities.Add(entityManager.SpawnEntity(color + "King", new MapCoordinates(x + i * separation, y, mapId)).Uid);
                        break;
                }
            }
        }

        // TODO: refactor to load FEN instead
        private void SpawnPawns(TabletopSession session, IEntityManager entityManager, string color, MapCoordinates left, float separation = 1f)
        {
            var (mapId, x, y) = left;

            for (int i = 0; i < 8; i++)
            {
                session.Entities.Add(entityManager.SpawnEntity(color + "Pawn", new MapCoordinates(x + i * separation, y, mapId)).Uid);
            }
        }
    }
}
