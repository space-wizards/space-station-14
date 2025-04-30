using Robust.Shared.Prototypes;
using Content.Shared.Maps;

namespace Content.Server.Spawners.Components;

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
    /// Probability that a particular decal gets spawned.
    /// </summary>
    [DataField]
    public float Prob = 1f;

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
    /// A random color to select from. Overrides Color if set.
    /// </summary>
    [DataField]
    public List<Color> RandomColorList = new();

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
    public List<ProtoId<ContentTileDefinition>> TileBlacklist = new();

    /// <summary>
    /// Sets whether to delete the entity with this component after the spawner is finished.
    /// </summary>
    [DataField]
    public bool DeleteSpawnerAfterSpawn = false;
}
