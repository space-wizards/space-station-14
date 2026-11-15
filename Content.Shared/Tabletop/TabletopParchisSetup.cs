using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopParchisSetup : TabletopSetup
{
    [DataField]
    public EntProtoId RedPiecePrototype = "RedTabletopPiece";

    [DataField]
    public EntProtoId GreenPiecePrototype = "GreenTabletopPiece";

    [DataField]
    public EntProtoId YellowPiecePrototype = "YellowTabletopPiece";

    [DataField]
    public EntProtoId BluePiecePrototype = "BlueTabletopPiece";

    public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
    {
        var board = entityManager.Spawn(BoardPrototype, session.Position);

        const float x1 = 6.25f;
        const float x2 = 4.25f;

        const float y1 = 6.25f;
        const float y2 = 4.25f;

        var center = session.Position;

        // Red pieces.
        var tempQualifier = entityManager.Spawn(RedPiecePrototype, center.Offset(-x1, -y1));
        session.Entities.Add(tempQualifier);
        var tempQualifier1 = entityManager.Spawn(RedPiecePrototype, center.Offset(-x1, -y2));
        session.Entities.Add(tempQualifier1);
        var tempQualifier2 = entityManager.Spawn(RedPiecePrototype, center.Offset(-x2, -y1));
        session.Entities.Add(tempQualifier2);
        var tempQualifier3 = entityManager.Spawn(RedPiecePrototype, center.Offset(-x2, -y2));
        session.Entities.Add(tempQualifier3);

        // Green pieces.
        var tempQualifier4 = entityManager.Spawn(GreenPiecePrototype, center.Offset(x1, -y1));
        session.Entities.Add(tempQualifier4);
        var tempQualifier5 = entityManager.Spawn(GreenPiecePrototype, center.Offset(x1, -y2));
        session.Entities.Add(tempQualifier5);
        var tempQualifier6 = entityManager.Spawn(GreenPiecePrototype, center.Offset(x2, -y1));
        session.Entities.Add(tempQualifier6);
        var tempQualifier7 = entityManager.Spawn(GreenPiecePrototype, center.Offset(x2, -y2));
        session.Entities.Add(tempQualifier7);

        // Yellow pieces.
        var tempQualifier8 = entityManager.Spawn(YellowPiecePrototype, center.Offset(x1, y1));
        session.Entities.Add(tempQualifier8);
        var tempQualifier9 = entityManager.Spawn(YellowPiecePrototype, center.Offset(x1, y2));
        session.Entities.Add(tempQualifier9);
        var tempQualifier10 = entityManager.Spawn(YellowPiecePrototype, center.Offset(x2, y1));
        session.Entities.Add(tempQualifier10);
        var tempQualifier11 = entityManager.Spawn(YellowPiecePrototype, center.Offset(x2, y2));
        session.Entities.Add(tempQualifier11);

        // Blue pieces.
        var tempQualifier12 = entityManager.Spawn(BluePiecePrototype, center.Offset(-x1, y1));
        session.Entities.Add(tempQualifier12);
        var tempQualifier13 = entityManager.Spawn(BluePiecePrototype, center.Offset(-x1, y2));
        session.Entities.Add(tempQualifier13);
        var tempQualifier14 = entityManager.Spawn(BluePiecePrototype, center.Offset(-x2, y1));
        session.Entities.Add(tempQualifier14);
        var tempQualifier15 = entityManager.Spawn(BluePiecePrototype, center.Offset(-x2, y2));
        session.Entities.Add(tempQualifier15);
    }
}
