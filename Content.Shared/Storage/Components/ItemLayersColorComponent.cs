namespace Content.Shared.Storage;


/// <summary>
/// Component that stores the color used to modify a specific sprite layer.
/// </summary>
/// <remarks>
/// The target layer is defined in the <c>ChangeLayersColorComponent</c>.
/// </remarks>
[RegisterComponent]
public sealed partial class ItemLayersColorComponent : Component
{
    /// <summary>
    /// The color that will be applied to the sprite layer defined in
    /// <c>ChangeLayersColorComponent</c>.
    /// </summary>
    [DataField]
    public Color Color = Color.White;
}
