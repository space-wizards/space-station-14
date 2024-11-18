namespace Content.Shared.Parallax.Biomes;

/// <summary>
/// Contains underlying biome layers.
/// </summary>
[DataRecord]
public sealed record NewBiomeMetaLayer
{
    /// <summary>
    /// Chunk dimensions for this meta layer.
    /// </summary>
    public Vector2i Size = new(8, 8);

    /// <summary>
    /// Meta layers that this one requires to be loaded first.
    /// Will ensure all of the chunks for our corresponding area are loaded.
    /// </summary>
    public List<string>? DependsOn;

    [DataField(required: true)]
    public List<INewBiomeLayer> SubLayers = new();
}

public interface INewBiomeLayer
{

}

[RegisterComponent]
public sealed partial class NewBiomeComponent : Component
{
    // TODO: Template prototype.

    [DataField(required: true)]
    public Dictionary<string, NewBiomeMetaLayer> Layers = new();

    public Dictionary<string, Dictionary<Vector2i, BiomeLoadedData>> LoadedData = new();

    /// <summary>
    /// Bounds loaded by players for this tick.
    /// </summary>
    public List<Box2i> LoadedBounds = new();

    /// <summary>
    /// Data that is currently being loaded.
    /// </summary>
    public Dictionary<string, HashSet<Vector2i>> PendingData = new();
}

public sealed class BiomeLoadedData
{
    public HashSet<EntityUid>? LoadedEntities;
    public List<uint>? LoadedDecals;
    public bool LoadedTiles;
}
