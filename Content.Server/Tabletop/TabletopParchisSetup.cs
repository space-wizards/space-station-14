using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop
{
    public class TabletopParchisSetup : TabletopSetup
    {
        [DataField("boardPrototype")]
        public string ParchisBoardPrototype { get; } = "ParchisBoardTabletop";

        [DataField("redPiecePrototype")]
        public string RedPiecePrototype { get; } = "RedParchisPiece";

        [DataField("greenPiecePrototype")]
        public string GreenPiecePrototype { get; } = "GreenParchisPiece";

        [DataField("yellowPiecePrototype")]
        public string YellowPiecePrototype { get; } = "YellowParchisPiece";

        [DataField("bluePiecePrototype")]
        public string BluePiecePrototype { get; } = "BlueParchisPiece";

        public override void SetupTabletop(MapId mapId, IEntityManager entityManager)
        {
            var board = entityManager.SpawnEntity(ParchisBoardPrototype, new MapCoordinates(0, 0, mapId));
            board.Transform.Anchored = true;

            const float x1 = 6.25f;
            const float x2 = 4.25f;

            const float y1 = 6.25f;
            const float y2 = 4.25f;

            // Red pieces.
            entityManager.SpawnEntity(RedPiecePrototype, new MapCoordinates(-x1, -y1, mapId));
            entityManager.SpawnEntity(RedPiecePrototype, new MapCoordinates(-x1, -y2, mapId));
            entityManager.SpawnEntity(RedPiecePrototype, new MapCoordinates(-x2, -y1, mapId));
            entityManager.SpawnEntity(RedPiecePrototype, new MapCoordinates(-x2, -y2, mapId));

            // Green pieces.
            entityManager.SpawnEntity(GreenPiecePrototype, new MapCoordinates(x1, -y1, mapId));
            entityManager.SpawnEntity(GreenPiecePrototype, new MapCoordinates(x1, -y2, mapId));
            entityManager.SpawnEntity(GreenPiecePrototype, new MapCoordinates(x2, -y1, mapId));
            entityManager.SpawnEntity(GreenPiecePrototype, new MapCoordinates(x2, -y2, mapId));

            // Yellow pieces.
            entityManager.SpawnEntity(YellowPiecePrototype, new MapCoordinates(x1, y1, mapId));
            entityManager.SpawnEntity(YellowPiecePrototype, new MapCoordinates(x1, y2, mapId));
            entityManager.SpawnEntity(YellowPiecePrototype, new MapCoordinates(x2, y1, mapId));
            entityManager.SpawnEntity(YellowPiecePrototype, new MapCoordinates(x2, y2, mapId));

            // Blue pieces.
            entityManager.SpawnEntity(BluePiecePrototype, new MapCoordinates(-x1, y1, mapId));
            entityManager.SpawnEntity(BluePiecePrototype, new MapCoordinates(-x1, y2, mapId));
            entityManager.SpawnEntity(BluePiecePrototype, new MapCoordinates(-x2, y1, mapId));
            entityManager.SpawnEntity(BluePiecePrototype, new MapCoordinates(-x2, y2, mapId));
        }
    }
}
