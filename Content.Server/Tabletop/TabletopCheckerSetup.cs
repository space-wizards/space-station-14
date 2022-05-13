using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed class TabletopCheckerSetup : TabletopSetup
    {
        [DataField("boardPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string CheckerBoardPrototype { get; } = "CheckerBoardTabletop";

        // TODO: Un-hardcode the rest of entity prototype IDs, probably.

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var checkerboard = entityManager.SpawnEntity(CheckerBoardPrototype, session.Position.Offset(-1, 0));

            session.Entities.Add(checkerboard);

            SpawnPieces(session, entityManager, session.Position.Offset(-4.5f, 3.5f));
        }

        private void SpawnPieces(TabletopSession session, IEntityManager entityManager, MapCoordinates topLeft, float separation = 1f)
        {
            var (mapId, x, y) = topLeft;

            // Spawn all black pieces
            SpawnPieces(session, entityManager, "Black", topLeft, separation);

            // Spawn all white pieces
            SpawnPieces(session, entityManager, "White", new MapCoordinates(x, y - 7 * separation, mapId), separation);

            // Queens
            for (int i = 1; i < 4; i++)
            {
                EntityUid tempQualifier = entityManager.SpawnEntity("BlackCheckerQueen", new MapCoordinates(x + 9 * separation + 9f / 32, y - i * separation, mapId));
                session.Entities.Add(tempQualifier);

                EntityUid tempQualifier1 = entityManager.SpawnEntity("WhiteCheckerQueen", new MapCoordinates(x + 8 * separation + 9f / 32, y - i * separation, mapId));
                session.Entities.Add(tempQualifier1);
            }

        }

        private void SpawnPieces(TabletopSession session, IEntityManager entityManager, string color, MapCoordinates left, float separation = 1f)
        {
          var (mapId, x, y) = left;
          // If white is being placed it must go from bottom->up
          var reversed = (color == "White") ? 1 : -1;

          for (int i = 0; i < 3; i++)
            {
            var x_offset = i % 2;
            if (reversed == -1) x_offset = 1 - x_offset; // Flips it

            for (int j = 0; j < 8; j += 2)
            {
              // Prevents an extra piece on the middle row
              if (x_offset + j > 8) continue;

              EntityUid tempQualifier4 = entityManager.SpawnEntity(color + "CheckerPiece", new MapCoordinates(x + (j + x_offset) * separation, y + i * reversed * separation, mapId));
              session.Entities.Add(tempQualifier4);
            }
          }
        }
    }
}
