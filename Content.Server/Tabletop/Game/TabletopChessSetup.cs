using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Tabletop.Game;

[UsedImplicitly]
public sealed partial class TabletopChessSetup : TabletopSetup
{
    // TODO: Un-hardcode the rest of entity prototype IDs, probably.
    [DataField]
    public EntProtoId BoardPrototype = default!;

    protected override void SetupTabletop(Spawner spawner)
    {
        spawner.Spawn(BoardPrototype, -1, 0);
        SpawnPieces(spawner.WithRelativeSpawnPosition(-4.5f, 3.5f));
    }

    private static void SpawnPieces(Spawner spawner)
    {
        // Spawn all black pieces
        SpawnPiecesRow(spawner, "Black");
        SpawnPawns(spawner.WithRelativeSpawnPosition(0, -1), "Black");

        // Spawn all white pieces
        SpawnPawns(spawner.WithRelativeSpawnPosition(0, -6), "White");
        SpawnPiecesRow(spawner.WithRelativeSpawnPosition(0, -7), "White");

        // Extra queens
        spawner.Spawn("BlackQueen", 9, -3);
        spawner.Spawn("WhiteQueen", 9, -4);
    }

    // TODO: refactor to load FEN instead
    private static void SpawnPiecesRow(Spawner spawner, string color)
    {
        // Piece arrangement with Forsyth–Edwards Notation (FEN) characters.
        const string piecesRow = "rnbqkbnr";
        var pieces = piecesRow.Select((p, column) =>
        {
            var piece = p switch
            {
                'r' => "Rook",
                'n' => "Knight",
                'b' => "Bishop",
                'q' => "Queen",
                'k' => "King",
                _ => throw new UnreachableException(),
            };
            return (piece, column);
        });
        foreach (var (piece, column) in pieces)
        {
            spawner.Spawn($"{color}{piece}", column, 0);
        }
    }

    // TODO: refactor to load FEN instead
    private static void SpawnPawns(Spawner spawner, string color)
    {
        foreach (var column in Enumerable.Range(0, 8))
        {
            spawner.Spawn($"{color}Pawn", column, 0);
        }
    }
}
