namespace Content.Shared.Humanoid;

// honestly, visualizers seem too constraining
// because this is literally just going to be sent
// in a field in AppearanceComponent
// whereas if you just directly dirtied the component
// and did state comparisons and acted when state
// differed, it would allow for changes that
// didn't do this?

public enum HumanoidVisualizerKey
{
    Key
}

public enum HumanoidVisualizerChangeKey
{
    /// <summary>
    ///     Skin color. Changes the skin tone of every
    ///     'skin' layer, including markings that follow
    ///     skin tone.
    /// </summary>
    SkinColor,
    /// <summary>
    ///     Base layer color. Changes the color of a
    ///     base layer.
    /// </summary>
    BaseLayerColor,
    /// <summary>
    ///     Layer visibility. Changes the visibility of
    ///     a single layer, including all markings.
    /// </summary>
    LayerVisibility,
    /// <summary>
    ///     Markings. This is the set of markings that
    ///     a humanoid has. An update to a marking on a set
    ///     will update all the markings on a humanoid.
    /// </summary>
    Markings
}

public sealed class HumanoidVisualizerChanges
{
    private List<HumanoidVisualizerChangeKey> _changed = new();
    private Color?
}
