using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public class TabletopBackgammonSetup : TabletopSetup
    {
        [DataField("boardPrototype")]
        public string BackgammonBoardPrototype { get; } = "BackgammonBoardTabletop";

        [DataField("whitePiecePrototype")]
        public string WhitePiecePrototype { get; } = "WhiteTabletopPiece";

        [DataField("blackPiecePrototype")]
        public string BlackPiecePrototype { get; } = "BlackTabletopPiece";
        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var board = entityManager.SpawnEntity(BackgammonBoardPrototype, session.Position);

            const float borderLengthX = 7.35f; //BORDER
            const float borderLengthY = 5.60f; //BORDER

            const float boardDistanceX = 1.25f;
            const float pieceDistanceY = 0.80f;

            float getXPosition(float distanceFromSide, bool isLeftSide)
            {
                var pos = borderLengthX - (distanceFromSide * boardDistanceX);
                return isLeftSide ? -pos : pos;
            }

            float getYPosition(float positionNumber, bool isTop)
            {
                var pos = borderLengthY - (pieceDistanceY * positionNumber);
                return isTop ? pos : -pos;
            }

            void addPieces(
                float distanceFromSide,
                int numberOfPieces,
                bool isBlackPiece,
                bool isTop,
                bool isLeftSide)
            {
                for (int i = 0; i < numberOfPieces; i++)
                {
                    session.Entities.Add(entityManager.SpawnEntity(isBlackPiece ? BlackPiecePrototype : WhitePiecePrototype, session.Position.Offset(getXPosition(distanceFromSide, isLeftSide), getYPosition(i, isTop))));
                }
            }

            // Top left
            addPieces(0, 5, true, true, true);
            // top middle left
            addPieces(4, 3, false, true, true);
            // top middle right
            addPieces(5, 5, false, true, false);
            // top far right
            addPieces(0, 2, true, true, false);
            // bottom left
            addPieces(0, 5, false, false, true);
            // bottom middle left
            addPieces(4, 3, true, false, true);
            // bottom middle right
            addPieces(5, 5, true, false, false);
            // bottom far right
            addPieces(0, 2, false, false, false);
        }
    }
}
