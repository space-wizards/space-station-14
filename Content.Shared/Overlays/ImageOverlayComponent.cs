using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Overlays;

/// <summary>
/// Adds a image overlay on screen when wearing an entity with this component.
/// The border pixels of the image get stretched out to cover the rest of the viewport.
/// </summary>
/// <remarks>
/// Need a blurred texture? Check out <see cref="Robust.Shared.Graphics.TextureLoadParameters"/> to include a filter sample.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ImageOverlayComponent : Component
{
    /// <summary>
    /// Path to image overlaid on the screen.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ResPath PathToOverlayImage = default!;

    /// <summary>
    /// The additional Color that can be overlaid over whole screen.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color AdditionalColorOverlay = new(0, 0, 0, 0);

    /// <summary>
    /// Scales the texture in x/y after it has been scaled to fit the user's viewport.
    /// </summary>
    /// <remarks>
    /// Avoid using large textures unless you really have to!
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Vector2 Scale = Vector2.One;

    /// <summary>
    /// Is this overlay active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;
}
