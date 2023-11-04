using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed partial class TabletopChessSetup : TabletopSetup
    {

        // TODO: Un-hardcode the rest of entity prototype IDs, probably.

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var chessboard = entityManager.SpawnEntity(BoardPrototype, session.Position.Offset(-1, 0));

            session.Entities.Add(chessboard);

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
            EntityUid tempQualifier = entityManager.SpawnEntity("BlackQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(tempQualifier);
            EntityUid tempQualifier1 = entityManager.SpawnEntity("WhiteQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - 4 * separation, mapId));
            session.Entities.Add(tempQualifier1);
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
                        EntityUid tempQualifier = entityManager.SpawnEntity(color + "Rook", new MapCoordinates(x + i * separation, y, mapId));
                        session.Entities.Add(tempQualifier);
                        break;
                    case 'n':
                        EntityUid tempQualifier1 = entityManager.SpawnEntity(color + "Knight", new MapCoordinates(x + i * separation, y, mapId));
                        session.Entities.Add(tempQualifier1);
                        break;
                    case 'b':
                        EntityUid tempQualifier2 = entityManager.SpawnEntity(color + "Bishop", new MapCoordinates(x + i * separation, y, mapId));
                        session.Entities.Add(tempQualifier2);
                        break;
                    case 'q':
                        EntityUid tempQualifier3 = entityManager.SpawnEntity(color + "Queen", new MapCoordinates(x + i * separation, y, mapId));
                        session.Entities.Add(tempQualifier3);
                        break;
                    case 'k':
                        EntityUid tempQualifier4 = entityManager.SpawnEntity(color + "King", new MapCoordinates(x + i * separation, y, mapId));
                        session.Entities.Add(tempQualifier4);
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
                EntityUid tempQualifier = entityManager.SpawnEntity(color + "Pawn", new MapCoordinates(x + i * separation, y, mapId));
                session.Entities.Add(tempQualifier);
            }
        }
    }
}
