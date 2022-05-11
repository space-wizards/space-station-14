using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics;

/// <summary>
///     Defined collision groups for the physics system.
/// </summary>
[Flags, PublicAPI]
[FlagsFor(typeof(CollisionLayer)), FlagsFor(typeof(CollisionMask))]
public enum CollisionGroup
{
    None             = 0,
    Opaque           = 1 << 0, // 1 Blocks light, can be hit by lasers
    Impassable       = 1 << 1, // 2 Walls, objects impassable by any means
    MidImpassable    = 1 << 2, // 4 Mobs, players, crabs, etc
    HighImpassable   = 1 << 3, // 8 Things on top of tables and things that block tall/large mobs.
    LowImpassable    = 1 << 4, // 16 For things that can fit under a table or squeeze under an airlock
    GhostImpassable  = 1 << 5, // 32 Things impassible by ghosts/observers, ie blessed tiles or forcefields
    BulletImpassable = 1 << 6, // 64 Can be hit by bullets

    MapGrid = MapGridHelpers.CollisionGroup, // Map grids, like shuttles. This is the actual grid itself, not the walls or other entities connected to the grid.

    // 32 possible groups
    AllMask = -1,

    MobMask = Impassable | MidImpassable | LowImpassable,
    MobLayer = Opaque | BulletImpassable,
    SmallMobMask = Impassable | LowImpassable,
    SmallMobLayer = Opaque | BulletImpassable,
    FlyingMobMask = Impassable,
    FlyingMobLayer = Opaque | BulletImpassable,

    LargeMobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    LargeMobLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable,

    MachineMask = Impassable | MidImpassable | LowImpassable,
    MachineLayer = Opaque | MidImpassable | LowImpassable | BulletImpassable,

    TableMask = Impassable | MidImpassable,
    TableLayer = MidImpassable,

    TabletopMachineMask = Impassable | HighImpassable,
    TabletopMachineLayer = Opaque | HighImpassable | BulletImpassable,

    GlassAirlockLayer = HighImpassable | MidImpassable | BulletImpassable,
    AirlockLayer = Opaque | GlassAirlockLayer,

    HumanoidBlockLayer = HighImpassable | MidImpassable,

    SlipLayer = MidImpassable | LowImpassable,
    ItemMask = Impassable | HighImpassable,
    ThrownItem = Impassable | HighImpassable,
    WallLayer = Opaque | Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable,
    GlassLayer = Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable,
    HalfWallLayer = MidImpassable | LowImpassable,
    FullTileMask = Impassable | HighImpassable | MidImpassable | LowImpassable,

    SubfloorMask = Impassable | LowImpassable
}
