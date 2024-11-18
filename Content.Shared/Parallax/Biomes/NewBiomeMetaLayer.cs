namespace Content.Shared.Parallax.Biomes;

/// <summary>
/// Contains underlying biome layers.
/// </summary>
[DataRecord]
public sealed record NewBiomeMetaLayer
{
    /// <summary>
    /// ID to refer to this meta layer from other meta layers.
    /// </summary>
    [DataField(required: true)]
    public string Id = string.Empty;

    /// <summary>
    /// Chunk dimensions for this meta layer.
    /// </summary>
    [DataField]
    public Vector2i Size = new(8, 8);

    /// <summary>
    /// Meta layers that this one requires to be loaded first.
    /// Will ensure all of the chunks for our corresponding area are loaded.
    /// </summary>
    [DataField]
    public List<string>? DependsOn;

    [DataField(required: true)]
    public List<INewBiomeMetaLayer> SubLayers = new();
}

public interface INewBiomeMetaLayer
{

}

[RegisterComponent]
public sealed partial class NewBiomeComponent : Component
{
    // TODO: Template prototype.

    [DataField(required: true)]
    public List<INewBiomeMetaLayer> Layers = new();

    public Dictionary<string, Dictionary<Vector2i, IBiomeLoadedData>> LoadedData = new();

    /// <summary>
    /// Data that is currently being loaded.
    /// </summary>
    public Dictionary<string, HashSet<Vector2i>> PendingData = new();
}

public interface IBiomeLoadedData
{
    /// <summary>
    /// Can the data be unloaded?
    /// </summary>
    bool Unloadable { get; set; }
}
