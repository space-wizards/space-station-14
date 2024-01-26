using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop
{
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
            EntityUid tempQualifier0 = entityManager.SpawnEntity(FigurineCatPrototype, center.Offset(-x1, y1));
            session.Entities.Add(tempQualifier0);

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

            for (float x = -5.25f; x <= -3.25f; x += 1f)
            {
                EntityUid tempQualifier = entityManager.SpawnEntity(FigurineGreenAutolatPrototype, center.Offset(x, y3_1));
                session.Entities.Add(tempQualifier);
            }

            for (float x = -5.25f; x <= -3.25f; x += 1f)
            {
                EntityUid tempQualifier = entityManager.SpawnEntity(FigurineGreenAutolatPrototype, center.Offset(x, y3_2));
                session.Entities.Add(tempQualifier);
            }

            // RedProtolat pieces.
            for (float x = 3.25f; x <= 5.25f; x += 1f)
            {
                EntityUid tempQualifier = entityManager.SpawnEntity(FigurineRedProtolatdPrototype, center.Offset(x, y3_1));
                session.Entities.Add(tempQualifier);
            }

            for (float x = 3.25f; x <= 5.25f; x += 1f)
            {
                EntityUid tempQualifier = entityManager.SpawnEntity(FigurineRedProtolatdPrototype, center.Offset(x, y3_2));
                session.Entities.Add(tempQualifier);
            }
        }
    }
}
