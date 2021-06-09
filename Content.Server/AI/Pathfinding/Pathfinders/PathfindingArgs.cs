using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.AI.Pathfinding.Pathfinders
{
    public struct PathfindingArgs
    {
        public EntityUid Uid { get; }
        public ICollection<string> Access { get; }
        public int CollisionMask { get; }
        public TileRef Start { get; }
        public TileRef End { get; }
        // How close we need to get to the endpoint to be 'done'
        public float Proximity { get; }
        // Whether we use cardinal only or not
        public bool AllowDiagonals { get; }
        // Can we go through walls
        public bool NoClip { get; }
        // Can we traverse space tiles
        public bool AllowSpace { get; }

        public PathfindingArgs(
            EntityUid entityUid,
            ICollection<string> access,
            int collisionMask,
            TileRef start,
            TileRef end,
            float proximity = 0.0f,
            bool allowDiagonals = true,
            bool noClip = false,
            bool allowSpace = false)
        {
            Uid = entityUid;
            Access = access;
            CollisionMask = collisionMask;
            Start = start;
            End = end;
            Proximity = proximity;
            AllowDiagonals = allowDiagonals;
            NoClip = noClip;
            AllowSpace = allowSpace;
        }
    }
}
