using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.GraphUpdates
{
    public struct CollidableMove : IPathfindingGraphUpdate
    {
        public int CollisionLayer { get; }
        public TileRef OldTile { get; }
        public TileRef NewTile { get; }

        public CollidableMove(int collisionLayer, TileRef oldTile, TileRef newTile)
        {
            CollisionLayer = collisionLayer;
            NewTile = newTile;
            OldTile = oldTile;
        }
    }
}
