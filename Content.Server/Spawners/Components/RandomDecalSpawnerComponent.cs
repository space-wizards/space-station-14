using Robust.Shared.Prototypes;
using Content.Shared.Maps;
using Content.Shared.Decals;

namespace Content.Server.Spawners.Components;

/// <summary>
/// This component spawns decals around the entity on MapInit.
/// See doc strings for the various parameters for more information.
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class RandomDecalSpawnerComponent : Component
{
    /// <summary>
    /// A list of decals to randomly select from when spawning.
    /// </summary>
    [DataField]
    public List<ProtoId<DecalPrototype>> Decals = new();

    /// <summary>
    /// Radius (in tiles) to spawn decals in. 0 will target only the tile the entity is on.
    /// </summary>
    [DataField]
    public float Radius = 1f;

    /// <summary>
    /// Probability that a particular decal gets spawned.
    /// </summary>
    [DataField]
    public float Prob = 1f;

    /// <summary>
    /// The maximum amount of decals to spawn across the entire radius.
    /// </summary>
    [DataField]
    public int MaxDecals = 1;

    /// <summary>
    /// The maximum amount of decals to spawn within a tile.
    /// </summary>
    /// <remarks>
    /// A value <= 0 or null is considered unlimited.
    /// </remarks>
    [DataField]
    public int? MaxDecalsPerTile = null;

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
    /// Whether decals should snap to the center omf a grid space or be placed randoly.
    /// </summary>
    /// <remarks>
    /// A null value will cause this to attempt to use the default value (DefaultSnap) for the decal.
    /// </remarks>
    [DataField]
    public bool? SnapPosition = false;

    /// <summary>
    /// zIndex for the generated decals
    /// </summary>
    [DataField]
    public int ZIndex = 0;

    /// <summary>
    /// Color for the generated decals. Does nothing if RandomColorList is set.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// A random color to select from. Overrides Color if set.
    /// </summary>
    [DataField]
    public List<Color>? RandomColorList = new();

    /// <summary>
    /// Whether the new decals are cleanable or not
    /// </summary>
    /// <remarks>
    /// A null value will cause this to attempt to use the default value (DefaultCleanable) for the decal.
    /// </remarks>
    [DataField]
    public bool? Cleanable = null;

    /// <summary>
    /// A list of tile prototype IDs to only place decals on.
    /// </summary>
    /// <remarks>
    /// Causes the TileBlacklist to be ignored if this is set.
    /// Note that due to the nature of tile-based placement, it's possible for decals to "spill over" onto nearby tiles.
    /// This is mostly so dirt decals don't go on diagonal tiles that won't work for them.
    /// </remarks>
    [DataField]
    public List<ProtoId<ContentTileDefinition>> TileWhitelist = new();

    /// <summary>
    /// A list of tile prototype IDs to avoid placing decals on.
    /// </summary>
    /// <remarks>
    /// Ignored if TileWhitelist is set.
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
