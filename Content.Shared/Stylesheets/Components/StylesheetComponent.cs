namespace Content.Shared.Stylesheets.Components;

/// <summary>
///     This component specifies a stylesheet to be used for the UI of the entity it is attached to. This component by
///     itself does not apply any stylesheets; it simply holds the name of the stylesheet to be used. To apply the
///     stylesheet, call <c>FancyWindow.ApplyStylesheetFrom()</c> in the BUI with the entity's <c>EntityUid</c>.
/// </summary>
[RegisterComponent]
public sealed partial class StylesheetComponent : Component
{
    /// <summary>
    /// The name of the stylesheet to use for this UI
    /// </summary>
    /// <remarks>
    /// Gets applied in the BUI when calling <c>FancyWindow.ApplyStylesheetFrom()</c>
    /// </remarks>
    [DataField]
    public string? Stylesheet = null;
}
