using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent, EntityCategory("Spawner")]
    public abstract partial class RandomDecalSpawnerComponent : Component
    {
        /// <summary>
        /// A list of decals to randomly select from when spawning.
        /// </summary>
        [DataField]
        public List<String> Decals = new();

        /// <summary>
        /// Radius (in tiles) to spawn decals in.
        /// </summary>
        [DataField]
        public float Range = 1f;

        /// <summary>
        /// Falloff factor for the range.
        /// When an attempt is made to place a decal, the position of the decal relative to
        /// the center of the spawner is divided by this value to determine the likelyhood of it being spawned.
        /// In other words, if rand() >= position/Falloff, where 0 <= rand() <= 1, and 0 <= position <= Range, spawn the decal
        /// A Falloff of 0 is allowed as a special case which guaruntees the maximum amount of spawns defined in MaxDecalsPerTile.
        /// </summary>
        [DataField]
        public float Falloff = 1f;

        /// <summary>
        /// Whether decals should have a random rotation applied to them.
        /// </summary>
        [DataField]
        public bool RandomRotation = false;

        /// <summary>
        /// Whether decals should snap to 90 degree orientations, does nothing if RandomRotation is false.
        /// </summary>
        [DataField]
        public bool SnapRotation = false;

        /// <summary>
        /// Whether decals should snap to the center of a grid space or be placed randomly within them.
        /// </summary>
        [DataField]
        public bool SnapPosition = false;

        /// <summary>
        /// zIndex for the generated decals
        /// </summary>
        [DataField]
        public int zIndex = 0;

        /// <summary>
        /// Color for the generated decals
        /// </summary>
        [DataField]
        public Color Color = Color.White;

        /// <summary>
        /// Whether the new decals are cleanable or not
        /// </summary>
        [DataField]
        public bool Cleanable = false;

        /// <summary>
        /// A list of tile names to avoid placing decals on.
        /// </summary>
        /// <remarks>
        /// Note that due to the nature of tile-based placement, it's possible for decals to "spill over" onto nearby tiles.
        /// This is mostly so dirt decals don't go on diagonal tiles that won't work for them.
        /// </remarks>
        [DataField]
        public List<String> TileBlacklist = new();

    }
}
