#nullable enable
using System;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using RobustPhysics = Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    /// <summary>
    ///     Defined collision groups for the physics system.
    /// </summary>
    [Flags, PublicAPI]
    [FlagsFor(typeof(CollisionLayer)), FlagsFor(typeof(CollisionMask))]
    public enum CollisionGroup
    {
		None            = 0,
		Opaque          = 1 <<  0, // 1 Blocks light, for lasers
		Impassable      = 1 <<  1, // 2 Walls, objects impassable by any means
		MobImpassable   = 1 <<  2, // 4 Mobs, players, crabs, etc
		VaultImpassable = 1 <<  3, // 8 Things that cannot be jumped over, not half walls or tables
		SmallImpassable = 1 <<  4, // 16 Things a smaller object - a cat, a crab - can't go through - a wall, but not a computer terminal or a table
        GhostImpassable = 1 <<  5, // 32 Things impassible by ghosts/observers, ie blessed tiles or forcefields
        Underplating    = 1 <<  6, // 64 Things that are under plating
        Passable        = 1 <<  7, // 128 Things that are passable
        MapGrid         = MapGridHelpers.CollisionGroup, // Map grids, like shuttles. This is the actual grid itself, not the walls or other entities connected to the grid.

        MobMask = Impassable | MobImpassable | VaultImpassable | SmallImpassable,
        ThrownItem = MobImpassable | Impassable,
        // 32 possible groups
        AllMask = -1,
    }
}
