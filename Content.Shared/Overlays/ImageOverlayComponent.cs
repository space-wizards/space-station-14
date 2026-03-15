using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Overlays;

/// <summary>
/// Adds a image overlay on screen when wearing an entity with this component.
/// </summary>
[RegisterComponent]
public sealed partial class ImageOverlayComponent : Component
{
    /// <summary>
    /// Path to image overlayed on the screen.
    /// </summary>
    [DataField(required: true)]
    public ResPath PathToOverlayImage = default!;

    /// <summary>
    /// The additional Color that can be overlayed over whole screen.
    /// </summary>
    [DataField]
    public Color AdditionalColorOverlay = new(0, 0, 0, 0);

    /// <summary>
    /// Is this overlay active
    /// </summary>
    [DataField]
    public bool Active = true;
}

