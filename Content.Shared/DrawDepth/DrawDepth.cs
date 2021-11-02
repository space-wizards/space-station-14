using Robust.Shared.Serialization;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Shared.DrawDepth
{
    [ConstantsFor(typeof(DrawDepthTag))]
    public enum DrawDepth
    {
        /// <summary>
        ///     This is for sub-floors, the floors you see after prying off a tile.
        /// </summary>
        LowFloors = DrawDepthTag.Default - 7,

        /// <summary>
        ///     Things that are beneath regular floors, such as wires.
        /// </summary>
        BelowFloor = DrawDepthTag.Default - 6,

        FloorTiles = DrawDepthTag.Default - 5,

        /// <summary>
        ///     Things that are actually right on the floor, like vents or puddles. This does not mean objects like
        ///     tables, though they are technically "on the floor".
        /// </summary>
        FloorObjects = DrawDepthTag.Default - 4,

        Walls = DrawDepthTag.Default - 3,

        /// <summary>
        ///     Used for windows (grilles use walls) and misc signage. Useful if you want to have an APC in the middle
        ///     of some wall-art or something.
        /// </summary>
        WallTops = DrawDepthTag.Default - 2,

        /// <summary>
        ///     Posters, APCs, air alarms, etc.
        /// </summary>
        WallMountedItems = DrawDepthTag.Default - 1,

        /// <summary>
        ///     Furniture, crates, etc.
        /// </summary>
        Objects = DrawDepthTag.Default,

        /// <summary>
        ///     In-between an furniture and an item. Useful for entities that need to appear on top of tables, but are
        ///     not items. E.g., power cell chargers. Also useful for pizza boxes, which appear above crates, but not
        ///     above the pizza itself.
        /// </summary>
        SmallObjects = DrawDepthTag.Default + 1,

        /// <summary>
        ///     Generic items. Things that should be above crates & tables, but underneath mobs.
        /// </summary>
        Items = DrawDepthTag.Default + 2,

        Mobs = DrawDepthTag.Default + 3,

        Doors = DrawDepthTag.Default + 4,

        /// <summary>
        ///     Explosions, fire, melee swings. Whatever.
        /// </summary>
        Effects = DrawDepthTag.Default + 5,

        Ghosts = DrawDepthTag.Default + 6,

        /// <summary>
        ///     Should be used for overlays (crit, dead, blinded, etc). Use this if it absolutely needs to be drawn
        ///     above anything else.
        /// </summary>
        Overlays = DrawDepthTag.Default + 7,
    }
}
