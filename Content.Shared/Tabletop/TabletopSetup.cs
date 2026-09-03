using System.Numerics;
using Content.Shared.Tabletop.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up a board for some tabletop game, like chess.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class TabletopSetup
{
    /// <summary>
    /// The entity prototype to spawn in for the board.
    /// </summary>
    [DataField]
    public EntProtoId BoardPrototype;

    /// <summary>
    /// Method for setting up a tabletop. Use this to spawn the board and pieces, etc.
    /// </summary>
    /// <param name="tabletop">The tabletop component being set up. You'll want to grab the tabletop center position here for spawning entities.</param>
    /// <param name="entityManager">Dependency that can be used for spawning entities.</param>
    public EntityUid SetupBoard(Entity<TabletopGameComponent> tabletop, MapCoordinates position, EntityManager entityManager)
    {
        var board = entityManager.PredictedSpawn(BoardPrototype, position);

        SetupPieces(tabletop, board, entityManager);

        return board;
    }

    /// <summary>
    /// Method for setting up the pieces for a board game. Use this to spawn the pieces, etc.
    /// </summary>
    /// <param name="tabletop">The entity of the "physical" board that can be picked up and moved.</param>
    /// <param name="board">The UID of the background entity in the board game map. Use this entity in <see cref="SpawnPiece"/> calls.</param>
    /// <param name="entityManager">Dependency that can be used for spawning entities.</param>
    public abstract void SetupPieces(Entity<TabletopGameComponent> tabletop, EntityUid board, EntityManager entityManager);

    /// <summary>
    /// Spawns a given piece at a given position relative to the given board.
    /// </summary>
    protected static void SpawnPiece(EntProtoId piece, Vector2 position, EntityUid board, EntityManager entityManager)
    {
        var pieceUid = entityManager.PredictedSpawnAttachedTo(piece, new(board, position));
        entityManager.EnsureComponent<TabletopDraggableComponent>(pieceUid);
    }
}
