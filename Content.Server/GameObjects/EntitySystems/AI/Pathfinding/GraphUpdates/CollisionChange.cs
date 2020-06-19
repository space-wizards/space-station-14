using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.GraphUpdates
{
    public class CollisionChange : IPathfindingGraphUpdate
    {
        public IEntity Owner { get; }
        public bool Value { get; }

        public CollisionChange(IEntity owner, bool value)
        {
            Owner = owner;
            Value = value;
        }
    }
}
