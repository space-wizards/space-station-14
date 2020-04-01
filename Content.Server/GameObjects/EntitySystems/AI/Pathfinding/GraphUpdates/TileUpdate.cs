using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.GraphUpdates
{
    public struct TileUpdate : IPathfindingGraphUpdate
    {
        public TileUpdate(TileRef tile)
        {
            Tile = tile;
        }

        public TileRef Tile { get; }
    }
}
