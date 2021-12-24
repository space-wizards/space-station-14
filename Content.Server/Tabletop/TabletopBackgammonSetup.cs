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

            float getXposition (float distanceFromSide, bool isLeftSide)
            {
                return isLeftSide ? -borderLengthX + (distanceFromSide * boardDistanceX)
                : borderLengthX - (distanceFromSide * boardDistanceX);
            }

            float getYPosition (float positionNumber, bool isTop)
            {
                return isTop ? borderLengthY  - (pieceDistanceY * positionNumber)
                : -borderLengthY  + (pieceDistanceY * positionNumber);
            }

            var center = session.Position;

            // Top left
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(0, true), getYPosition(0, true))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(0, true), getYPosition(1, true))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(0, true), getYPosition(2, true))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(0, true), getYPosition(3, true))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(0, true), getYPosition(4, true))));

            // top middle left
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(4, true), getYPosition(0, true))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(4, true), getYPosition(1, true))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(4, true), getYPosition(2, true))));

            // top middle right
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(5, false), getYPosition(0, true))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(5, false), getYPosition(1, true))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(5, false), getYPosition(2, true))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(5, false), getYPosition(3, true))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(5, false), getYPosition(4, true))));

            // top far right
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(0, false), getYPosition(0, true))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(0, false), getYPosition(1, true))));


            // bottom left
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(0, true), getYPosition(0, false))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(0, true), getYPosition(1, false))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(0, true), getYPosition(2, false))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(0, true), getYPosition(3, false))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(0, true), getYPosition(4, false))));

            // bottom middle left
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(4, true), getYPosition(0, false))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(4, true), getYPosition(1, false))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(4, true), getYPosition(2, false))));

           // bottom middle right
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(5, false), getYPosition(0, false))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(5, false), getYPosition(1, false))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(5, false), getYPosition(2, false))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(5, false), getYPosition(3, false))));
            session.Entities.Add(entityManager.SpawnEntity(BlackPiecePrototype, center.Offset(getXposition(5, false), getYPosition(4, false))));

            // bottom far right
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(0, false), getYPosition(0, false))));
            session.Entities.Add(entityManager.SpawnEntity(WhitePiecePrototype, center.Offset(getXposition(0, false), getYPosition(1, false))));
        }
    }
}
