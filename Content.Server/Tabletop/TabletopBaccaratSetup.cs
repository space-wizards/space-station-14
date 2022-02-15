using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public class TabletopBaccaratSetup : TabletopSetup
    {
        [DataField("boardPrototype")]
        public string BaccaratTablePrototype { get; } = "BaccaratTabletop";

        [DataField("playingCardPrototype")]
        public string PlayingCardPrototype { get; } = "PlayingCard";

        [DataField("chipPrototype")]
        public string ChipPrototype { get; } = "Chip";
        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var baccaratTable = entityManager.SpawnEntity(BaccaratTablePrototype, session.Position);

            const float borderLengthX = 7.35f; //BORDER
            const float borderLengthY = 5.60f; //BORDER

            const float boardDistanceX = 1.25f;
            const float pieceDistanceY = 0.80f;

            float GetXPosition(float distanceFromSide, bool isLeftSide)
            {
                var pos = borderLengthX - (distanceFromSide * boardDistanceX);
                return isLeftSide ? -pos : pos;
            }

            float GetYPosition(float positionNumber, bool isTop)
            {
                var pos = borderLengthY - (pieceDistanceY * positionNumber);
                return isTop ? pos : -pos;
            }

            SpawnChips(session, entityManager, session.Position.Offset(-4.5f, 3.5f));
            // // Top left
            // AddPieces(0, 5, true, true, true);
            // // top middle left
            // AddPieces(4, 3, false, true, true);
            // // top middle right
            // AddPieces(5, 5, false, true, false);
            // // top far right
            // AddPieces(0, 2, true, true, false);
            // // bottom left
            // AddPieces(0, 5, false, false, true);
            // // bottom middle left
            // AddPieces(4, 3, true, false, true);
            // // bottom middle right
            // AddPieces(5, 5, true, false, false);
            // // bottom far right
            // AddPieces(0, 2, false, false, false);
        }

        private void SpawnChips(TabletopSession session, IEntityManager entityManager, MapCoordinates topLeft, float separation = 1f) {
            var (mapId, x, y) = topLeft;

            EntityUid redChip1 = entityManager.SpawnEntity("RedChip", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(redChip1);
            EntityUid redChip2 = entityManager.SpawnEntity("RedChip", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(redChip2);

            EntityUid blueChip1 = entityManager.SpawnEntity("BlueChip", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(blueChip1);
            EntityUid blueChip2 = entityManager.SpawnEntity("BlueChip", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(blueChip2);

            EntityUid greenChip1 = entityManager.SpawnEntity("GreenChip", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(greenChip1);
            EntityUid greenChip2 = entityManager.SpawnEntity("GreenChip", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(greenChip2);

            EntityUid yellowChip1 = entityManager.SpawnEntity("YellowChip", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(yellowChip1);
            EntityUid yellowChip2 = entityManager.SpawnEntity("YellowChip", new MapCoordinates(x + 9 * separation + 9f / 32, y - 3 * separation, mapId));
            session.Entities.Add(yellowChip2);
        }
    }
}
