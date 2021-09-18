using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
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

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var board = entityManager.SpawnEntity(ParchisBoardPrototype, session.Position);

            const float x1 = 6.25f;
            const float x2 = 4.25f;

            const float y1 = 6.25f;
            const float y2 = 4.25f;

            var center = session.Position;

            // Red pieces.
            session.Entities.Add(entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y1)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y2)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y1)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y2)).Uid);

            // Green pieces.
            session.Entities.Add(entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y1)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y2)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y1)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y2)).Uid);

            // Yellow pieces.
            session.Entities.Add(entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y1)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y2)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y1)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y2)).Uid);

            // Blue pieces.
            session.Entities.Add(entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y1)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y2)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y1)).Uid);
            session.Entities.Add(entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y2)).Uid);
        }
    }
}
