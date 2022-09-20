using Robust.Shared.Map;

namespace Content.Server.NPC.Pathfinding;

public sealed class PathfindingResultEvent : EntityEventArgs
{
    public Queue<EntityCoordinates> Path = new();
    // TODO: EntityCoordinates to target
    // TODO: Partial
}
