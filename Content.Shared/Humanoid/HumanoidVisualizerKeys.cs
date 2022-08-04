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

public enum HumanoidVisualizerDataKey
{
    /// <summary>
    ///     Current species. This is primarily for fetching
    ///     all species sprites for a character, including
    ///     any base species sprites. Server authoritative:
    ///     clients do not have to worry about validating
    ///     this.
    /// </summary>
    Species,
    /// <summary>
    ///     Custom base layers on this humanoid. Overrides
    ///     base layers on a species.
    /// </summary>
    CustomBaseLayer,
    /// <summary>
    ///     Skin color. Changes the skin tone of every
    ///     'skin' layer, including markings that follow
    ///     skin tone.
    /// </summary>
    SkinColor,
    /// <summary>
    ///     Eye color. Changes the color of a human's eye.
    /// </summary>
    EyeColor,
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
