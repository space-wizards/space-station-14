using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
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
    public override void SetupTabletop(TabletopGameComponent tabletop, MapCoordinates coordinates, EntityManager entityManager)
    {
        tabletop.Board = entityManager.SpawnEntity(BoardPrototype, coordinates);

        const float x1 = 6.25f;
        const float x2 = 4.25f;

        const float y1 = 6.25f;
        const float y2 = 4.25f;

        // Red pieces.
        SpawnPiece(RedPiecePrototype, new(-x1, -y1), tabletop, entityManager);
        SpawnPiece(RedPiecePrototype, new(-x1, -y2), tabletop, entityManager);
        SpawnPiece(RedPiecePrototype, new(-x2, -y1), tabletop, entityManager);
        SpawnPiece(RedPiecePrototype, new(-x2, -y2), tabletop, entityManager);

        // Green pieces.
        SpawnPiece(GreenPiecePrototype, new(x1, -y1), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x1, -y2), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x2, -y1), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x2, -y2), tabletop, entityManager);

        // Yellow pieces.
        SpawnPiece(GreenPiecePrototype, new(x1, y1), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x1, y2), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x2, y1), tabletop, entityManager);
        SpawnPiece(GreenPiecePrototype, new(x2, y2), tabletop, entityManager);

        // Blue pieces.
        SpawnPiece(BluePiecePrototype, new(-x1, y1), tabletop, entityManager);
        SpawnPiece(BluePiecePrototype, new(-x1, y2), tabletop, entityManager);
        SpawnPiece(BluePiecePrototype, new(-x2, y1), tabletop, entityManager);
        SpawnPiece(BluePiecePrototype, new(-x2, y2), tabletop, entityManager);
    }
}
