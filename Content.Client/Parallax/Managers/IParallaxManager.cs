namespace Content.Client.Parallax.Managers;

public interface IParallaxManager
{
    /// <summary>
    /// All WorldHomePosition values are offset by this.
    /// </summary>
    Vector2 ParallaxAnchor { get; set; }

    /// <summary>
    /// The layers of the selected parallax.
    /// </summary>
    ParallaxLayerPrepared[] GetParallaxLayers(string name);

    /// <summary>
    /// Loads in the default parallax to use.
    /// Do not call until prototype manager is available.
    /// </summary>
    void LoadDefaultParallax();
}

