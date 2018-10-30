using System;

namespace Content.Shared.Physics
{
    /// <summary>
    ///     Defined collision groups for the physics system.
    /// </summary>
    [Flags]
    public enum CollisionGroup
    {
        None = 0x0000,
        Grid = 0x0001, // Walls
        Mob = 0x0002, // Mobs, like the player or NPCs
        Fixture = 0x0004, // wall fixtures, like APC or posters
        Items = 0x008, // Items on the ground
    }
}
