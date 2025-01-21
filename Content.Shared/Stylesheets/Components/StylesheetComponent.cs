namespace Content.Shared.Stylesheets.Components;

[RegisterComponent]
public sealed partial class StylesheetComponent : Component
{
    /// <summary>
    /// The name of the stylesheet to use for this UI
    /// </summary>
    /// <remarks>
    /// Gets applied in the BUI when calling <see cref="" />
    /// </remarks>
    [DataField]
    public string? Stylesheet = null;
}
