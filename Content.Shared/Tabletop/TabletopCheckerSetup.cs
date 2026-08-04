using System.Numerics;
using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up a game of checkers on a checkerboard.
/// Assumes each square is 1 m wide.
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopCheckersSetup : TabletopSetup
{
    [DataField]
    public EntProtoId PrototypePieceWhite = "CheckerPieceRed";

    [DataField]
    public EntProtoId PrototypeCrownWhite = "CheckerCrownRed";

    [DataField]
    public EntProtoId PrototypePieceBlack = "CheckerPieceBlack";

    [DataField]
    public EntProtoId PrototypeCrownBlack = "CheckerCrownBlack";

    // The coordinates of the center of the left bottom corner square.
    public const float PieceOffsetX = -4.5f;
    public const float PieceOffsetY = 3.5f;

    public override void SetupTabletop(TabletopGameComponent tabletop, MapCoordinates coordinates, IEntityManager entityManager)
    {
        tabletop.Board = entityManager.Spawn(BoardPrototype, coordinates);

        SpawnPieces(tabletop, entityManager);
    }

    private void SpawnPieces(TabletopGameComponent tabletop, IEntityManager entityManager)
    {
        Vector2 left = new(PieceOffsetX, PieceOffsetY);
        // Pieces.
        for (var offsetY = 0; offsetY < 3; offsetY++)
        {
            var checker = offsetY % 2;

            // Offset by checker: prevents an extra piece on the middle row.
            for (var offsetX = 0; offsetX < 8 - checker; offsetX += 2)
            {
                SpawnPiece(PrototypePieceBlack, new(left.X + offsetX + (1 - checker), left.Y - offsetY), tabletop, entityManager);
                SpawnPiece(PrototypePieceWhite, new(left.X + offsetX + checker, left.Y + offsetY - 7), tabletop, entityManager);
            }
        }

        const int numKings = 3;
        const int numSpares = 6;
        const float overlap = 0.25f;
        const float xOffsetBlack = 9 + 2f / 32;
        const float xOffsetWhite = 8 + 7f / 32;

        // Kings.
        for (var i = 0; i < numKings; i++)
        {
            var step = -(overlap * i);
            SpawnPiece(PrototypeCrownBlack, new(left.X + xOffsetBlack, left.Y + step), tabletop, entityManager);
            SpawnPiece(PrototypeCrownWhite, new(left.X + xOffsetWhite, left.Y + step), tabletop, entityManager);
        }

        // Spares.
        for (var i = 0; i < numSpares; i++)
        {
            var step = -(overlap * (numKings + 2) + overlap * i);
            SpawnPiece(PrototypeCrownBlack, new(left.X + xOffsetBlack, left.Y + step), tabletop, entityManager);
            SpawnPiece(PrototypeCrownWhite, new(left.X + xOffsetWhite, left.Y + step), tabletop, entityManager);
        }
    }
}
