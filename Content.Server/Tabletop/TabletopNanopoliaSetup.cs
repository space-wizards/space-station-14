using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed partial class TabletopNanopoliaSetup : TabletopSetup
    {

        [DataField("FigurineCatPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string FigurineCatPrototype { get; private set; } = "FigurineCat";

        [DataField("FigurineLaceupsPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string FigurineLaceupsPrototype { get; private set; } = "FigurineLaceups";

        [DataField("FigurineMcgriffPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string FigurineMcgriffPrototype { get; private set; } = "FigurineMcgriff";

        [DataField("FigurineTopHatPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string FigurineTopHatPrototype { get; private set; } = "FigurineTopHat";

        [DataField("FigurineGreenAutolatPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string FigurineGreenAutolatPrototype { get; private set; } = "FigurineGreenAutolathe";

        [DataField("FigurineRedProtolatdPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string FigurineRedProtolatdPrototype { get; private set; } = "FigurineRedProtolathe";

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var board = entityManager.SpawnEntity(BoardPrototype, session.Position);

            const float x1 = 7.25f;
            const float x2 = 6.25f;

            const float y1 = 7.25f;
            const float y2 = 6.25f;

            const float y3_1 = 5.25f;
            const float y3_2 = 4.25f;

            var center = session.Position;

            // Cat pieces.
            EntityUid tempQualifier = entityManager.SpawnEntity(FigurineCatPrototype, center.Offset(-x1, y1));
            session.Entities.Add(tempQualifier);

            // Laceup pieces.
            EntityUid tempQualifier1 = entityManager.SpawnEntity(FigurineLaceupsPrototype, center.Offset(-x1, y2));
            session.Entities.Add(tempQualifier1);

            // Mcgriff pieces.
            EntityUid tempQualifier2 = entityManager.SpawnEntity(FigurineMcgriffPrototype, center.Offset(-x2, y1));
            session.Entities.Add(tempQualifier2);

            // TopHat pieces.
            EntityUid tempQualifier3 = entityManager.SpawnEntity(FigurineTopHatPrototype, center.Offset(-x2, y2));
            session.Entities.Add(tempQualifier3);

            // GreenAutolat pieces.
            EntityUid tempQualifier4 = entityManager.SpawnEntity(FigurineGreenAutolatPrototype, center.Offset(-5.25f, y3_1));
            session.Entities.Add(tempQualifier4);

            EntityUid tempQualifier5 = entityManager.SpawnEntity(FigurineGreenAutolatPrototype, center.Offset(-4.25f, y3_1));
            session.Entities.Add(tempQualifier5);

            EntityUid tempQualifier6 = entityManager.SpawnEntity(FigurineGreenAutolatPrototype, center.Offset(-3.25f, y3_1));
            session.Entities.Add(tempQualifier6);

            EntityUid tempQualifier7 = entityManager.SpawnEntity(FigurineGreenAutolatPrototype, center.Offset(-5.25f, y3_2));
            session.Entities.Add(tempQualifier7);

            EntityUid tempQualifier8 = entityManager.SpawnEntity(FigurineGreenAutolatPrototype, center.Offset(-4.25f, y3_2));
            session.Entities.Add(tempQualifier8);

            EntityUid tempQualifier9 = entityManager.SpawnEntity(FigurineGreenAutolatPrototype, center.Offset(-3.25f, y3_2));
            session.Entities.Add(tempQualifier9);

            // RedProtolat pieces.
            EntityUid tempQualifier10 = entityManager.SpawnEntity(FigurineRedProtolatdPrototype, center.Offset(5.25f, y3_1));
            session.Entities.Add(tempQualifier10);
            EntityUid tempQualifier11 = entityManager.SpawnEntity(FigurineRedProtolatdPrototype, center.Offset(4.25f, y3_1));
            session.Entities.Add(tempQualifier11);
            EntityUid tempQualifier12 = entityManager.SpawnEntity(FigurineRedProtolatdPrototype, center.Offset(3.25f, y3_1));
            session.Entities.Add(tempQualifier12);

            EntityUid tempQualifier13 = entityManager.SpawnEntity(FigurineRedProtolatdPrototype, center.Offset(5.25f, y3_2));
            session.Entities.Add(tempQualifier13);
            EntityUid tempQualifier14 = entityManager.SpawnEntity(FigurineRedProtolatdPrototype, center.Offset(4.25f, y3_2));
            session.Entities.Add(tempQualifier14);
            EntityUid tempQualifier15 = entityManager.SpawnEntity(FigurineRedProtolatdPrototype, center.Offset(3.25f, y3_2));
            session.Entities.Add(tempQualifier15);
        }
    }
}
