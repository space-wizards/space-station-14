using System.Numerics;
using Content.Shared.Tabletop.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop;

[ImplicitDataDefinitionForInheritors]
public abstract partial class TabletopSetup
{
    /// <summary>
    /// The prototype to spawn in for the board.
    /// </summary>
    [DataField]
    public EntProtoId BoardPrototype;

    /// <summary>
    ///  Method for setting up a tabletop. Use this to spawn the board and pieces, etc.
    ///  Make sure you add every entity you create to the Entities hashset in the session.
    /// </summary>
    /// <param name="tabletop">The tabletop component being set up. You'll want to grab the tabletop center position here for spawning entities.</param>
    /// <param name="entityManager">Dependency that can be used for spawning entities.</param>
    public abstract void SetupTabletop(TabletopGameComponent tabletop, MapCoordinates position, IEntityManager entityManager);

    /// <summary>
    /// Convenience function: spawns a given piece at a given position and adds it to the session given.
    /// </summary>
    protected void SpawnPiece(EntProtoId piece, Vector2 position, TabletopGameComponent tabletop, IEntityManager entityManager)
    {
        entityManager.SpawnAttachedTo(piece, new(tabletop.Board!.Value, position));
    }
}
