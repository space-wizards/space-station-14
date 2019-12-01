using System;
using JetBrains.Annotations;

namespace Content.Shared.Physics
{
    /// <summary>
    ///     Defined collision groups for the physics system.
    /// </summary>
    [Flags, PublicAPI]
    public enum CollisionGroup
    {
		None            = 0,
		Opaque          = 1 <<  0, // 1 Blocks light, for lasers
		Impassable      = 1 <<  1, // 2 Walls, objects impassable by any means
		MobImpassable   = 1 <<  2, // 4 Mobs, players, crabs, etc
		VaultImpassable = 1 <<  3, // 8 Things that cannot be jumped over, not half walls or tables
		SmallImpassable = 1 <<  4, // 16 Things a smaller object - a cat, a crab - can't go through - a wall, but not a computer terminal or a table
        Clickable       = 1 <<  5, // 32 Temporary "dummy" layer to ensure that objects can still be clicked even if they don't collide with anything (you can't interact with objects that have no layer, including items)

        // 32 possible groups
        MobMask = Impassable | MobImpassable | VaultImpassable | SmallImpassable,
        AllMask = -1,
    }
}
