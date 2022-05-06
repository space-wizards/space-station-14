using System;
using Robust.Client.Graphics;
using Content.Client.Parallax;

namespace Content.Client.Parallax.Managers;

public interface IParallaxManager
{
    /// <summary>
    /// The current parallax.
    /// Changing this causes a new parallax to be loaded (eventually).
    /// Do not alter until prototype manager is available.
    /// Useful "csi" input for testing new parallaxes:
    /// using Content.Client.Parallax.Managers; IoCManager.Resolve<IParallaxManager>().ParallaxName = "test";
    /// </summary>
    string ParallaxName { get; set; }

    /// <summary>
    /// All WorldHomePosition values are offset by this.
    /// </summary>
    Vector2 ParallaxAnchor { get; set; }

    /// <summary>
    /// The layers of the currently loaded parallax.
    /// This will change on a whim without notification.
    /// </summary>
    ParallaxLayerPrepared[] ParallaxLayers { get; }

    /// <summary>
    /// Used to initialize the manager.
    /// Do not call until prototype manager is available.
    /// </summary>
    void LoadParallax();
}

