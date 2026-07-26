using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up checkers on a checkerboard.
/// Assumes each square is 1 m wide.
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopCheckerSetup : TabletopSetup
{
    [DataField]
    public EntProtoId PrototypePieceWhite = "CheckerPieceRed";

    [DataField]
    public EntProtoId PrototypeCrownWhite = "CheckerCrownRed";

    [DataField]
    public EntProtoId PrototypePieceBlack = "CheckerPieceBlack";

    [DataField]
    public EntProtoId PrototypeCrownBlack = "CheckerCrownBlack";

    public override void SetupTabletop(TabletopGameComponent tabletop, IEntityManager entityManager)
    {
        SpawnPiece(BoardPrototype, tabletop.Position, tabletop, entityManager);

        SpawnPieces(tabletop, entityManager, tabletop.Position.Offset(-4.5f, 3.5f));
    }

    private void SpawnPieces(TabletopGameComponent tabletop, IEntityManager entityManager, MapCoordinates left)
    {
        // Pieces.
        for (var offsetY = 0; offsetY < 3; offsetY++)
        {
            var checker = offsetY % 2;

            // Offset by checker: prevents an extra piece on the middle row.
            for (var offsetX = 0; offsetX < 8 - checker; offsetX += 2)
            {
                SpawnPiece(PrototypePieceBlack, left.Offset(offsetX + (1 - checker), -offsetY), tabletop, entityManager);
                SpawnPiece(PrototypePieceWhite, left.Offset(offsetX + checker, offsetY - 7), tabletop, entityManager);
            }
        }

        const int numCrowns = 3;
        const int numSpares = 6;
        const float overlap = 0.25f;
        const float xOffsetBlack = 9 + 2f / 32;
        const float xOffsetWhite = 8 + 7f / 32;

        // Crowns.
        for (var i = 0; i < numCrowns; i++)
        {
            var step = -(overlap * i);
            SpawnPiece(PrototypeCrownBlack, left.Offset(xOffsetBlack, step), tabletop, entityManager);
            SpawnPiece(PrototypeCrownWhite, left.Offset(xOffsetWhite, step), tabletop, entityManager);
        }

        // Spares.
        for (var i = 0; i < numSpares; i++)
        {
            var step = -(overlap * (numCrowns + 2) + overlap * i);
            SpawnPiece(PrototypeCrownBlack, left.Offset(xOffsetBlack, step), tabletop, entityManager);
            SpawnPiece(PrototypeCrownWhite, left.Offset(xOffsetWhite, step), tabletop, entityManager);
        }
    }
}
