using System.Numerics;
using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up a game of checkers on a checkerboard.
/// </summary>
/// <remarks>
/// Assumes each square is 1 m wide.
/// </remarks>
[UsedImplicitly]
public sealed partial class TabletopCheckersSetup : TabletopSetup
{
    /// <summary>
    /// The entity prototype for the red player's checkers.
    /// </summary>
    [DataField]
    public EntProtoId PrototypePieceRed = "CheckersPieceRed";

    /// <summary>
    /// The entity prototype for the red player's kings.
    /// </summary>
    [DataField]
    public EntProtoId PrototypeKingRed = "CheckersKingRed";

    /// <summary>
    /// The entity prototype for the black player's checkers.
    /// </summary>
    [DataField]
    public EntProtoId PrototypePieceBlack = "CheckersPieceBlack";

    /// <summary>
    /// The entity prototype for the black player's kings.
    /// </summary>
    [DataField]
    public EntProtoId PrototypeKingBlack = "CheckersKingBlack";

    // The coordinates of the center of the left bottom corner square.
    public const float PieceOffsetX = -4.5f;
    public const float PieceOffsetY = 3.5f;

    public override void SetupPieces(Entity<TabletopGameComponent> tabletop, EntityUid board, EntityManager entityManager)
    {
        Vector2 left = new(PieceOffsetX, PieceOffsetY);

        // Setup main pieces.
        for (var offsetY = 0; offsetY < 3; offsetY++)
        {
            var checker = offsetY % 2;

            // Offset by checker: prevents an extra piece on the middle row.
            for (var offsetX = 0; offsetX < 8 - checker; offsetX += 2)
            {
                SpawnPiece(PrototypePieceBlack, new(left.X + offsetX + (1 - checker), left.Y - offsetY), board, entityManager);
                SpawnPiece(PrototypePieceRed, new(left.X + offsetX + checker, left.Y + offsetY - 7), board, entityManager);
            }
        }

        const int numKings = 3;
        const int numSpares = 6;
        const float overlap = 0.25f;
        const float xOffsetBlack = 9 + 2f / 32;
        const float xOffsetWhite = 8 + 7f / 32;

        // Setup extra kings.
        for (var i = 0; i < numKings; i++)
        {
            var step = -(overlap * i);
            SpawnPiece(PrototypeKingBlack, new(left.X + xOffsetBlack, left.Y + step), board, entityManager);
            SpawnPiece(PrototypeKingRed, new(left.X + xOffsetWhite, left.Y + step), board, entityManager);
        }

        // Setup extra spares.
        for (var i = 0; i < numSpares; i++)
        {
            var step = -(overlap * (numKings + 2) + overlap * i);
            SpawnPiece(PrototypePieceBlack, new(left.X + xOffsetBlack, left.Y + step), board, entityManager);
            SpawnPiece(PrototypePieceRed, new(left.X + xOffsetWhite, left.Y + step), board, entityManager);
        }
    }
}
