
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop;

public sealed partial class TabletopNanopoliaSetup : TabletopSetup
{

    [DataField]
    public EntProtoId figurineCatPrototype = "FigurineCat";

    [DataField]
    public EntProtoId figurineLaceupsPrototype = "FigurineLaceups";

    [DataField]
    public EntProtoId figurineMcgriffPrototype = "FigurineMcgriff";

    [DataField]
    public EntProtoId figurineTopHatPrototype = "FigurineTopHat";

    [DataField]
    public EntProtoId figurineGreenAutolatPrototype = "FigurineGreenAutolathe";

    [DataField]
    public EntProtoId figurineRedProtolatdPrototype = "FigurineRedProtolathe";

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
        EntityUid tempQualifier0 = entityManager.SpawnEntity(figurineCatPrototype, center.Offset(-x1, y1));
        session.Entities.Add(tempQualifier0);

        // Laceup pieces.
        EntityUid tempQualifier1 = entityManager.SpawnEntity(figurineLaceupsPrototype, center.Offset(-x1, y2));
        session.Entities.Add(tempQualifier1);

        // Mcgriff pieces.
        EntityUid tempQualifier2 = entityManager.SpawnEntity(figurineMcgriffPrototype, center.Offset(-x2, y1));
        session.Entities.Add(tempQualifier2);

        // TopHat pieces.
        EntityUid tempQualifier3 = entityManager.SpawnEntity(figurineTopHatPrototype, center.Offset(-x2, y2));
        session.Entities.Add(tempQualifier3);

        // GreenAutolat pieces.

        for (float x = -5.25f; x <= -3.25f; x += 1f)
        {
            EntityUid tempQualifier = entityManager.SpawnEntity(figurineGreenAutolatPrototype, center.Offset(x, y3_1));
            session.Entities.Add(tempQualifier);
        }

        for (float x = -5.25f; x <= -3.25f; x += 1f)
        {
            EntityUid tempQualifier = entityManager.SpawnEntity(figurineGreenAutolatPrototype, center.Offset(x, y3_2));
            session.Entities.Add(tempQualifier);
        }

        // RedProtolat pieces.
        for (float x = 3.25f; x <= 5.25f; x += 1f)
        {
            EntityUid tempQualifier = entityManager.SpawnEntity(figurineRedProtolatdPrototype, center.Offset(x, y3_1));
            session.Entities.Add(tempQualifier);
        }

        for (float x = 3.25f; x <= 5.25f; x += 1f)
        {
            EntityUid tempQualifier = entityManager.SpawnEntity(figurineRedProtolatdPrototype, center.Offset(x, y3_2));
            session.Entities.Add(tempQualifier);
        }
    }
}
