using Robust.Shared.Prototypes;

namespace Content.Client.Parallax.Data;

/// <summary>
/// Prototype data for a parallax.
/// </summary>
[Prototype("parallax")]
public readonly record struct ParallaxPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    /// <summary>
    /// Parallax layers.
    /// </summary>
    [DataField("layers")]
    public List<ParallaxLayerConfig> Layers { get; } = new();

    /// <summary>
    /// Parallax layers, low-quality.
    /// </summary>
    [DataField("layersLQ")]
    public List<ParallaxLayerConfig> LayersLQ { get; } = new();

    /// <summary>
    /// If low-quality layers don't exist for this parallax and high-quality should be used instead.
    /// </summary>
    [DataField("layersLQUseHQ")]
    public bool LayersLQUseHQ { get; } = true;
}
