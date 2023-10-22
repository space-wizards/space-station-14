using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed partial class TabletopParchisSetup : TabletopSetup
    {

        [DataField("redPiecePrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string RedPiecePrototype { get; private set; } = "RedTabletopPiece";

        [DataField("greenPiecePrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string GreenPiecePrototype { get; private set; } = "GreenTabletopPiece";

        [DataField("yellowPiecePrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string YellowPiecePrototype { get; private set; } = "YellowTabletopPiece";

        [DataField("bluePiecePrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BluePiecePrototype { get; private set; } = "BlueTabletopPiece";

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var board = entityManager.SpawnEntity(BoardPrototype, session.Position);

            const float x1 = 6.25f;
            const float x2 = 4.25f;

            const float y1 = 6.25f;
            const float y2 = 4.25f;

            var center = session.Position;

            // Red pieces.
            EntityUid tempQualifier = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y1));
            session.Entities.Add(tempQualifier);
            EntityUid tempQualifier1 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y2));
            session.Entities.Add(tempQualifier1);
            EntityUid tempQualifier2 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y1));
            session.Entities.Add(tempQualifier2);
            EntityUid tempQualifier3 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y2));
            session.Entities.Add(tempQualifier3);

            // Green pieces.
            EntityUid tempQualifier4 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y1));
            session.Entities.Add(tempQualifier4);
            EntityUid tempQualifier5 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y2));
            session.Entities.Add(tempQualifier5);
            EntityUid tempQualifier6 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y1));
            session.Entities.Add(tempQualifier6);
            EntityUid tempQualifier7 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y2));
            session.Entities.Add(tempQualifier7);

            // Yellow pieces.
            EntityUid tempQualifier8 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y1));
            session.Entities.Add(tempQualifier8);
            EntityUid tempQualifier9 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y2));
            session.Entities.Add(tempQualifier9);
            EntityUid tempQualifier10 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y1));
            session.Entities.Add(tempQualifier10);
            EntityUid tempQualifier11 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y2));
            session.Entities.Add(tempQualifier11);

            // Blue pieces.
            EntityUid tempQualifier12 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y1));
            session.Entities.Add(tempQualifier12);
            EntityUid tempQualifier13 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y2));
            session.Entities.Add(tempQualifier13);
            EntityUid tempQualifier14 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y1));
            session.Entities.Add(tempQualifier14);
            EntityUid tempQualifier15 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y2));
            session.Entities.Add(tempQualifier15);
        }
    }
}
