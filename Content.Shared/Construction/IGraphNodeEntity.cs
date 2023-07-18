using Robust.Shared.Prototypes;

namespace Content.Shared.Construction;

public interface IGraphNodeEntity
{
    /// <summary>
    ///     Gets the <see cref="EntityPrototype"/> ID for a node, given the <see cref="EntityUid"/> of both the
    ///     construction entity and the user entity.
    ///     If the construction entity is null, then we are dealing with a "start construction" for an entity that
    ///     does not exist yet.
    ///     If the user entity is null, this node was reached through means other some sort of "user interaction".
    /// </summary>
    /// <param name="uid">Uid of the construction entity.</param>
    /// <param name="userUid">Uid of the user that caused the transition to the node.</param>
    /// <param name="args">Arguments with useful instances, etc.</param>
    /// <returns></returns>
    public string? GetId(EntityUid? uid, EntityUid? userUid, GraphNodeEntityArgs args);
}

public readonly struct GraphNodeEntityArgs
{
    public readonly IEntityManager EntityManager;

    public GraphNodeEntityArgs(IEntityManager entityManager)
    {
        EntityManager = entityManager;
    }
}
