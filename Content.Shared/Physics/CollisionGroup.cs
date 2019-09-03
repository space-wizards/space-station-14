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
		None      = 0,
		Grid      = 1 <<  0, // Walls
		Mob       = 1 <<  1, // Mobs, like the player or NPCs
		Fixture   = 1 <<  2, // wall fixtures, like APC or posters
		Items     = 1 <<  3, // Items on the ground
		Furniture = 1 <<  4, // Tables, machines

        // 32 possible groups
        MobMask = Grid | Mob | Furniture,
        AllMask = -1,
    }
}
