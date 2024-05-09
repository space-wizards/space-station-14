using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics;

/// <summary>
///     Defined collision groups for the physics system.
///     Mask is what it collides with when moving. Layer is what CollisionGroup it is part of.
/// </summary>
[Flags, PublicAPI]
[FlagsFor(typeof(CollisionLayer)), FlagsFor(typeof(CollisionMask))]
public enum CollisionGroup
{
    None               = 0,
    Opaque             = 1 << 0, // 1 Blocks light, can be hit by lasers
    Impassable         = 1 << 1, // 2 Walls, objects impassable by any means
    MidImpassable      = 1 << 2, // 4 Mobs, players, crabs, etc
    HighImpassable     = 1 << 3, // 8 Things on top of tables and things that block tall/large mobs.
    LowImpassable      = 1 << 4, // 16 For things that can fit under a table or squeeze under an airlock
    GhostImpassable    = 1 << 5, // 32 Things impassible by ghosts/observers, ie blessed tiles or forcefields
    BulletImpassable   = 1 << 6, // 64 Can be hit by bullets
    InteractImpassable = 1 << 7, // 128 Blocks interaction/InRangeUnobstructed
    DoorPassable       = 1 << 8, // 256 Allows door to close over top, Like blast doors over conveyors for disposals rooms/cargo.

    MapGrid = MapGridHelpers.CollisionGroup, // Map grids, like shuttles. This is the actual grid itself, not the walls or other entities connected to the grid.

    // 32 possible groups
    AllMask = -1,

    // Humanoids, etc.
    MobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    MobLayer = Opaque | BulletImpassable,
    // Mice, drones
    SmallMobMask = Impassable | LowImpassable,
    SmallMobLayer = Opaque | BulletImpassable,
    // Birds/other small flyers
    FlyingMobMask = Impassable | HighImpassable,
    FlyingMobLayer = Opaque | BulletImpassable,

    // Mechs
    LargeMobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    LargeMobLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable,

    // Machines, computers
    MachineMask = Impassable | MidImpassable | LowImpassable,
    MachineLayer = Opaque | MidImpassable | LowImpassable | BulletImpassable,
    ConveyorMask = Impassable | MidImpassable | LowImpassable | DoorPassable,

    // Crates
    CrateMask = Impassable | HighImpassable | LowImpassable,

    // Tables that SmallMobs can go under
    TableMask = Impassable | MidImpassable,
    TableLayer = MidImpassable,

    // Tabletop machines, windoors, firelocks
    TabletopMachineMask = Impassable | HighImpassable,
    // Tabletop machines
    TabletopMachineLayer = Opaque | HighImpassable | BulletImpassable,

    // Airlocks, windoors, firelocks
    GlassAirlockLayer = HighImpassable | MidImpassable | BulletImpassable | InteractImpassable,
    AirlockLayer = Opaque | GlassAirlockLayer,

    // Airlock assembly
    HumanoidBlockLayer = HighImpassable | MidImpassable,

    // Soap, spills
    SlipLayer = MidImpassable | LowImpassable,
    ItemMask = Impassable | HighImpassable,
    ThrownItem = Impassable | HighImpassable | BulletImpassable,
    WallLayer = Opaque | Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,
    GlassLayer = Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,
    HalfWallLayer = MidImpassable | LowImpassable,

    // Statue, monument, airlock, window
    FullTileMask = Impassable | HighImpassable | MidImpassable | LowImpassable | InteractImpassable,
    // FlyingMob can go past
    FullTileLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,

    SubfloorMask = Impassable | LowImpassable
}
