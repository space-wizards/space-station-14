using Robust.Shared.Serialization;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Shared.GameObjects
{
    [ConstantsFor(typeof(DrawDepthTag))]
    public enum DrawDepth
    {
        LowFloors = DrawDepthTag.Default - 7,
        /// <summary>
        /// Things that are beneath regular floors, such as wires.
        /// </summary>
        BelowFloor = DrawDepthTag.Default - 6,
        FloorTiles = DrawDepthTag.Default - 5,
        /// <summary>
        /// Things that are actually right on the floor, like vents.
        /// </summary>
        FloorObjects = DrawDepthTag.Default - 4,
        Walls = DrawDepthTag.Default - 3,
        WallTops = DrawDepthTag.Default - 2,
        WallMountedItems = DrawDepthTag.Default - 1,
        Objects = DrawDepthTag.Default,
        Items = DrawDepthTag.Default + 1,
        Mobs = DrawDepthTag.Default + 2,
        Effects = DrawDepthTag.Default + 3,
        Ghosts = DrawDepthTag.Default + 4,
        Overlays = DrawDepthTag.Default + 5,
    }
}
