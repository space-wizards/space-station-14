using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public class TabletopBaccaratSetup : TabletopSetup
    {
        [DataField("boardPrototype")]
        public string BaccaratTablePrototype { get; } = "BaccaratTableTabletop";

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
    }
}
