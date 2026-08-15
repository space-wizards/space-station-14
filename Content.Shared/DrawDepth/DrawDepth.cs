using Robust.Shared.Serialization;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Shared.DrawDepth
{
    [ConstantsFor(typeof(DrawDepthTag))]
    public enum DrawDepth : byte
    {
        ContentDefault = 32,

        /// <summary>
        ///     This is for sub-floors, the floors you see after prying off a tile.
        /// </summary>
        LowFloors = ContentDefault - 22,

        // various entity types that require different
        // draw depths, as to avoid hiding
        // If updating this set, update the gaps above Puddles and Overdoors.
        #region SubfloorEntities
        ThickPipe = ContentDefault - 21,
        ThickWire = ContentDefault - 20,
        ThinPipeAlt2 = ContentDefault - 19,
        ThinPipeAlt1 = ContentDefault - 18,
        ThinPipe = ContentDefault - 17,
        ThinWire = ContentDefault - 16,
        #endregion

        /// <summary>
        ///     Things that are beneath regular floors.
        /// </summary>
        BelowFloor = ContentDefault - 15,

        /// <summary>
        ///     Used for entities like carpets.
        /// </summary>
        FloorTiles = ContentDefault - 14,

        /// <summary>
        ///     Things that are actually right on the floor, like ice crust or atmos devices. This does not mean objects like
        ///     tables, even though they are technically "on the floor".
        /// </summary>
        FloorObjects = ContentDefault - 13,

        /// <summary>
        ///     Discrete drawdepth to avoid z-fighting with other FloorObjects but also above floor entities.
        /// </summary>
        Puddles = ContentDefault - 12,

        // NOTE: There's a gap for subfloor entities to retain relative draw depth when revealed by a t-ray scanner (need 6 layers in between).

        /// <summary>
        ///     Objects that are on the floor, but should render above puddles. This includes kudzu, holopads, telepads and levers.
        /// </summary>
        HighFloorObjects = ContentDefault - 5,

        DeadMobs = ContentDefault - 4,

        /// <summary>
        ///     Allows small mobs like mice and drones to render under tables and chairs but above puddles and vents
        /// </summary>
        SmallMobs = ContentDefault - 3,

        Walls = ContentDefault - 2,

        /// <summary>
        ///     Used for windows (grilles use walls) and misc signage. Useful if you want to have an APC in the middle
        ///     of some wall-art or something.
        /// </summary>
        WallTops = ContentDefault - 1,

        /// <summary>
        ///     Furniture, crates, tables. etc. If an entity should be drawn on top of a table, it needs a draw depth
        ///     that is higher than this.
        /// </summary>
        Objects = ContentDefault,

        /// <summary>
        ///     In-between an furniture and an item. Useful for entities that need to appear on top of tables, but are
        ///     not items. E.g., power cell chargers. Also useful for pizza boxes, which appear above crates, but not
        ///     above the pizza itself.
        /// </summary>
        SmallObjects = ContentDefault + 1,

        /// <summary>
        ///     Posters, APCs, air alarms, etc. This also includes most lights & lamps.
        /// </summary>
        WallMountedItems = ContentDefault + 2,

        /// <summary>
        ///     To use for objects that would usually fall under SmallObjects, but appear taller than 1 tile. For example: Reagent Grinder
        /// </summary>
        LargeObjects = ContentDefault + 3,

        /// <summary>
        ///     Generic items. Things that should be above crates & tables, but underneath mobs.
        /// </summary>
        Items = ContentDefault + 4,
        /// <summary>
        /// Stuff that should be drawn below mobs, but on top of items. Like muzzle flash.
        /// </summary>
        BelowMobs = ContentDefault + 5,

        Mobs = ContentDefault + 6,

        OverMobs = ContentDefault + 7,

        Doors = ContentDefault + 8,

        /// <summary>
        /// Blast doors and shutters which go over the usual doors.
        /// </summary>
        BlastDoors = ContentDefault + 9,

        /// <summary>
        /// Stuff that needs to draw over most things, but not effects, like Kudzu.
        /// </summary>
        Overdoors = ContentDefault + 10,

        // NOTE: There's a gap here for subfloor layers in mapping mode (need 6 layers in between)

        /// <summary>
        ///     Visible atmos gas.
        /// </summary>
        Gasses = ContentDefault + 17,

        /// <summary>
        ///     Explosions, fire, melee swings. Whatever.
        /// </summary>
        Effects = ContentDefault + 18,

        Ghosts = ContentDefault + 19,

        /// <summary>
        ///    Use this selectively if it absolutely needs to be drawn above (almost) everything else. Examples include
        ///    the pointing arrow, the drag & drop ghost-entity, and some debug tools.
        /// </summary>
        Overlays = ContentDefault + 20,
    }
}
