using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopParchisSetup : TabletopSetup
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string RedPiecePrototype = "RedTabletopPiece";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GreenPiecePrototype = "GreenTabletopPiece";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string YellowPiecePrototype = "YellowTabletopPiece";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BluePiecePrototype = "BlueTabletopPiece";

    public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
    {
        var board = entityManager.SpawnEntity(BoardPrototype, session.Position);

        const float x1 = 6.25f;
        const float x2 = 4.25f;

        const float y1 = 6.25f;
        const float y2 = 4.25f;

        var center = session.Position;

        // Red pieces.
        var tempQualifier = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y1));
        session.Entities.Add(tempQualifier);
        var tempQualifier1 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x1, -y2));
        session.Entities.Add(tempQualifier1);
        var tempQualifier2 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y1));
        session.Entities.Add(tempQualifier2);
        var tempQualifier3 = entityManager.SpawnEntity(RedPiecePrototype, center.Offset(-x2, -y2));
        session.Entities.Add(tempQualifier3);

        // Green pieces.
        var tempQualifier4 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y1));
        session.Entities.Add(tempQualifier4);
        var tempQualifier5 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x1, -y2));
        session.Entities.Add(tempQualifier5);
        var tempQualifier6 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y1));
        session.Entities.Add(tempQualifier6);
        var tempQualifier7 = entityManager.SpawnEntity(GreenPiecePrototype, center.Offset(x2, -y2));
        session.Entities.Add(tempQualifier7);

        // Yellow pieces.
        var tempQualifier8 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y1));
        session.Entities.Add(tempQualifier8);
        var tempQualifier9 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x1, y2));
        session.Entities.Add(tempQualifier9);
        var tempQualifier10 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y1));
        session.Entities.Add(tempQualifier10);
        var tempQualifier11 = entityManager.SpawnEntity(YellowPiecePrototype, center.Offset(x2, y2));
        session.Entities.Add(tempQualifier11);

        // Blue pieces.
        var tempQualifier12 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y1));
        session.Entities.Add(tempQualifier12);
        var tempQualifier13 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x1, y2));
        session.Entities.Add(tempQualifier13);
        var tempQualifier14 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y1));
        session.Entities.Add(tempQualifier14);
        var tempQualifier15 = entityManager.SpawnEntity(BluePiecePrototype, center.Offset(-x2, y2));
        session.Entities.Add(tempQualifier15);
    }
}
