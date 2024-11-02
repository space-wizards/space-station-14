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
    /// Chunks for this meta layer.
    /// </summary>
    [DataField]
    public Vector2i Size = new(32, 32);

    /// <summary>
    /// Meta layers that this one requires to be loaded first.
    /// </summary>
    [DataField]
    public string? DependsOn;

    [DataField(required: true)]
    public List<INewBiomeMetaLayer> Layers = new();
}

[RegisterComponent]
public sealed partial class NewBiomeComponent : Component
{
    // TODO: Template prototype.

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
