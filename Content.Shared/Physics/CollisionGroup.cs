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
    None                  = 0,
    LowOpaque             = 1 << 0, // 1 Blocks light and lasers close to the ground (most entities except those on top of tables or flying)
    MidOpaque             = 1 << 1, // 2 Blocks light and lasers at normal height (any entities that are not small and don't fly)
    HighOpaque            = 1 << 2, // 4 Blocks light and lasers high up (large or flying entities)
    Impassable            = 1 << 3, // 2 Walls, objects impassable by any means
    LowImpassable         = 1 << 4, // 8 Blocks passage close to the ground. (Such as squeezing under a table or airlock)
    MidImpassable         = 1 << 5, // 16 Blocks passage at normal height (Most mobs, players, crabs, etc)
    HighImpassable        = 1 << 6, // 32 Blocks passage high up (Things on top of tables, tall/large mobs, flying mobs)
    GhostImpassable       = 1 << 7, // 64 Things impassible by ghosts/observers, ie blessed tiles or forcefields
    LowBulletImpassable   = 1 << 8, // 128 Blocks bullets and projectiles close to the ground (most entities except those on top of tables or flying)
    MidBulletImpassable   = 1 << 9, // 256 Blocks bullets and projectiles at normal height (any entities that are not small and don't fly)
    HighBulletImpassable  = 1 << 10, // 512 Blocks bullets and projectiles high up (large or flying entities)
    InteractImpassable    = 1 << 11, // 1024 Blocks interaction/InRangeUnobstructed
    DoorPassable          = 1 << 12, // 2048 Allows door to close over top, Like blast doors over conveyors for disposals rooms/cargo.

    FullOpaque = LowOpaque | MidOpaque | HighOpaque,
    FullBulletImpassable = LowBulletImpassable | MidBulletImpassable | HighBulletImpassable,

    // Legacy
    Opaque = FullOpaque,
    BulletImpassable = FullBulletImpassable,

    MapGrid = MapGridHelpers.CollisionGroup, // Map grids, like shuttles. This is the actual grid itself, not the walls or other entities connected to the grid.

    // 32 possible groups
    AllMask = -1,


    //TODO: Kill these, test stuff
    BulletMask_Low = Impassable | LowBulletImpassable,
    BulletMask_High = Impassable | HighImpassable,



    // Projectiles
    BulletMask = Impassable | FullBulletImpassable,

    // Humanoids, etc.
    MobMask = Impassable | HighImpassable | MidImpassable | LowImpassable, // why are they high?
    MobLayer = FullOpaque | FullBulletImpassable,
    // Mice, drones
    SmallMobMask = Impassable | LowImpassable,
    SmallMobLayer = FullOpaque | FullBulletImpassable,
    // Birds/other small flyers
    FlyingMobMask = Impassable | HighImpassable,
    FlyingMobLayer = FullOpaque | FullBulletImpassable,

    // Mechs
    LargeMobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    LargeMobLayer = FullOpaque | HighImpassable | MidImpassable | LowImpassable | FullBulletImpassable,

    // Machines, computers
    MachineMask = Impassable | MidImpassable | LowImpassable,
    MachineLayer = FullOpaque | MidImpassable | LowImpassable | FullBulletImpassable,
    ConveyorMask = Impassable | MidImpassable | LowImpassable | DoorPassable,

    // Tables that SmallMobs can go under
    TableMask = Impassable | MidImpassable,
    TableLayer = MidImpassable,

    // Tabletop machines, windoors, firelocks
    TabletopMachineMask = Impassable | HighImpassable,
    // Tabletop machines
    TabletopMachineLayer = FullOpaque | HighImpassable | FullBulletImpassable,

    // Airlocks, windoors, firelocks
    GlassAirlockLayer = HighImpassable | MidImpassable | FullBulletImpassable | InteractImpassable,
    AirlockLayer = FullOpaque | GlassAirlockLayer,

    // Airlock assembly
    HumanoidBlockLayer = HighImpassable | MidImpassable,

    // Soap, spills
    SlipLayer = MidImpassable | LowImpassable,
    ItemMask = Impassable | HighImpassable,
    ThrownItem = Impassable | HighImpassable | FullBulletImpassable,
    WallLayer = FullOpaque | Impassable | HighImpassable | MidImpassable | LowImpassable | FullBulletImpassable | InteractImpassable,
    GlassLayer = Impassable | HighImpassable | MidImpassable | LowImpassable | FullBulletImpassable | InteractImpassable,
    HalfWallLayer = MidImpassable | LowImpassable, // probably make this low-mid opaque?

    // Statue, monument, airlock, window
    FullTileMask = Impassable | HighImpassable | MidImpassable | LowImpassable | InteractImpassable,
    // FlyingMob can go past
    FullTileLayer = FullOpaque |  HighImpassable | MidImpassable | LowImpassable | FullBulletImpassable | InteractImpassable,

    SubfloorMask = Impassable | LowImpassable
}
