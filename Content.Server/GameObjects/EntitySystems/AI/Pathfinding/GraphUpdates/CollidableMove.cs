using Robust.Shared.GameObjects.Components.Transform;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.GraphUpdates
{
    public struct CollidableMove : IPathfindingGraphUpdate
    {
        public MoveEvent MoveEvent { get; }

        public CollidableMove(MoveEvent moveEvent)
        {
            MoveEvent = moveEvent;
        }
    }
}
