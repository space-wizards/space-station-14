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
            EntityUidtempQualifier = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y1));
            session.Entities.Add(tempQualifier);
            EntityUidtempQualifier1 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y2));
            session.Entities.Add(tempQualifier1);
            EntityUidtempQualifier2 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y1));
            session.Entities.Add(tempQualifier2);
            EntityUidtempQualifier3 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y2));
            session.Entities.Add(tempQualifier3);

            // Green pieces.
            EntityUidtempQualifier4 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y1));
            session.Entities.Add(tempQualifier4);
            EntityUidtempQualifier5 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y2));
            session.Entities.Add(tempQualifier5);
            EntityUidtempQualifier6 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y1));
            session.Entities.Add(tempQualifier6);
            EntityUidtempQualifier7 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y2));
            session.Entities.Add(tempQualifier7);

            // Yellow pieces.
            EntityUidtempQualifier8 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y1));
            session.Entities.Add(tempQualifier8);
            EntityUidtempQualifier9 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y2));
            session.Entities.Add(tempQualifier9);
            EntityUidtempQualifier10 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y1));
            session.Entities.Add(tempQualifier10);
            EntityUidtempQualifier11 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y2));
            session.Entities.Add(tempQualifier11);

            // Blue pieces.
            EntityUidtempQualifier12 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y1));
            session.Entities.Add(tempQualifier12);
            EntityUidtempQualifier13 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y2));
            session.Entities.Add(tempQualifier13);
            EntityUidtempQualifier14 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y1));
            session.Entities.Add(tempQualifier14);
            EntityUidtempQualifier15 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y2));
            session.Entities.Add(tempQualifier15);
        }
    }
}
