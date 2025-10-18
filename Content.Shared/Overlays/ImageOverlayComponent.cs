using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Overlays;

/// <summary>
/// Adds a image based shader when wearing an entity with this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImageOverlayComponent : Component
{
    /// <summary>
    /// Path to image overlayed on the screen.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ResPath PathToOverlayImage = default!;

    /// <summary>
    /// The additional Color that can be overlayed over whole screen.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color AdditionalColorOverlay = new(0, 0, 0, 0);
}

