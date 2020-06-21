using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.GraphUpdates
{
    public sealed class AccessReaderChangeUpdate : IPathfindingGraphUpdate
    {
        public IEntity Entity { get; }
        public bool Enabled { get; }

        public AccessReaderChangeUpdate(IEntity entity, bool enabled)
        {
            Entity = entity;
            Enabled = enabled;
        }
    }
}