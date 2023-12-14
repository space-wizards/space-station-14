using JetBrains.Annotations;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed partial class TabletopBackgammonSetup : TabletopSetup
    {

        [DataField("whitePiecePrototype")]
        public string WhitePiecePrototype { get; private set; } = "WhiteTabletopPiece";

        [DataField("blackPiecePrototype")]
        public string BlackPiecePrototype { get; private set; } = "BlackTabletopPiece";
        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var board = entityManager.SpawnEntity(BoardPrototype, session.Position);

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

            void AddPieces(
                float distanceFromSide,
                int numberOfPieces,
                bool isBlackPiece,
                bool isTop,
                bool isLeftSide)
            {
                for (int i = 0; i < numberOfPieces; i++)
                {
                    session.Entities.Add(entityManager.SpawnEntity(isBlackPiece ? BlackPiecePrototype : WhitePiecePrototype, session.Position.Offset(GetXPosition(distanceFromSide, isLeftSide), GetYPosition(i, isTop))));
                }
            }

            // Top left
            AddPieces(0, 5, true, true, true);
            // top middle left
            AddPieces(4, 3, false, true, true);
            // top middle right
            AddPieces(5, 5, false, true, false);
            // top far right
            AddPieces(0, 2, true, true, false);
            // bottom left
            AddPieces(0, 5, false, false, true);
            // bottom middle left
            AddPieces(4, 3, true, false, true);
            // bottom middle right
            AddPieces(5, 5, true, false, false);
            // bottom far right
            AddPieces(0, 2, false, false, false);
        }
    }
}
