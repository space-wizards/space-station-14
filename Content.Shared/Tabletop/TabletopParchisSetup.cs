using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up a board game to play Parchis.
/// </summary>
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

    /// <inheritdoc />
    public override void SetupTabletop(TabletopGameComponent tabletop, IEntityManager entityManager)
    {
        if (tabletop.Position is not { } position)
            return;

        entityManager.SpawnEntity(BoardPrototype, position);

        const float x1 = 6.25f;
        const float x2 = 4.25f;

        const float y1 = 6.25f;
        const float y2 = 4.25f;

        // Red pieces.
        SpawnPiece(RedPiecePrototype, position.Offset(-x1, -y1), tabletop, entityManager);
        SpawnPiece(RedPiecePrototype, position.Offset(-x1, -y2), tabletop, entityManager);
        SpawnPiece(RedPiecePrototype, position.Offset(-x2, -y1), tabletop, entityManager);
        SpawnPiece(RedPiecePrototype, position.Offset(-x2, -y2), tabletop, entityManager);

        // Green pieces.
        SpawnPiece(GreenPiecePrototype, position.Offset(x1, -y1), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, position.Offset(x1, -y2), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, position.Offset(x2, -y1), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, position.Offset(x2, -y2), tabletop, entityManager);

        // Yellow pieces.
        SpawnPiece(GreenPiecePrototype, position.Offset(x1, y1), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, position.Offset(x1, y2), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, position.Offset(x2, y1), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, position.Offset(x2, y2), tabletop, entityManager);

        // Blue pieces.
        SpawnPiece(BluePiecePrototype, position.Offset(-x1, y1), tabletop, entityManager);
        SpawnPiece(BluePiecePrototype, position.Offset(-x1, y2), tabletop, entityManager);
        SpawnPiece(BluePiecePrototype, position.Offset(-x2, y1), tabletop, entityManager);
        SpawnPiece(BluePiecePrototype, position.Offset(-x2, y2), tabletop, entityManager);
    }
}
