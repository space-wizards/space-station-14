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
            IEntity tempQualifier = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y1));
            session.Entities.Add(tempQualifier);
            IEntity tempQualifier1 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y2));
            session.Entities.Add(tempQualifier1);
            IEntity tempQualifier2 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y1));
            session.Entities.Add(tempQualifier2);
            IEntity tempQualifier3 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y2));
            session.Entities.Add(tempQualifier3);

            // Green pieces.
            IEntity tempQualifier4 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y1));
            session.Entities.Add(tempQualifier4);
            IEntity tempQualifier5 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y2));
            session.Entities.Add(tempQualifier5);
            IEntity tempQualifier6 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y1));
            session.Entities.Add(tempQualifier6);
            IEntity tempQualifier7 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y2));
            session.Entities.Add(tempQualifier7);

            // Yellow pieces.
            IEntity tempQualifier8 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y1));
            session.Entities.Add(tempQualifier8);
            IEntity tempQualifier9 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y2));
            session.Entities.Add(tempQualifier9);
            IEntity tempQualifier10 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y1));
            session.Entities.Add(tempQualifier10);
            IEntity tempQualifier11 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y2));
            session.Entities.Add(tempQualifier11);

            // Blue pieces.
            IEntity tempQualifier12 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y1));
            session.Entities.Add(tempQualifier12);
            IEntity tempQualifier13 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y2));
            session.Entities.Add(tempQualifier13);
            IEntity tempQualifier14 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y1));
            session.Entities.Add(tempQualifier14);
            IEntity tempQualifier15 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y2));
            session.Entities.Add(tempQualifier15);
        }
    }
}
