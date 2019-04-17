using System;

namespace Content.Shared.Physics
{
    /// <summary>
    ///     Defined collision groups for the physics system.
    /// </summary>
    [Flags]
    public enum CollisionGroup
    {
        None = 0,
        Grid = 1, // Walls
        Mob = 2, // Mobs, like the player or NPCs
        Fixture = 4, // wall fixtures, like APC or posters
        Items = 8 // Items on the ground
    }
}
