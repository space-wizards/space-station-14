using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.Label;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A button intended for use with a monotone color palette
/// </summary>
public sealed class MonotoneButton : Button
{
    /// <summary>
    /// Specifies the color of the label text when the button is pressed.
    /// </summary>
    [ViewVariables]
    public Color AltTextColor { set; get; } = new Color(0.2f, 0.2f, 0.2f);

    public MonotoneButton()
    {
        RemoveStyleClass("button");
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        // Recolor the label
        if (Label != null)
            Label.ModulateSelfOverride = DrawMode == DrawModeEnum.Pressed ? AltTextColor : null;

        // Modulate the button if disabled
        Modulate = Disabled ? Color.Gray : Color.White;
    }

    protected override void StylePropertiesChanged()
    {
        base.StylePropertiesChanged();
        UpdateAppearance();
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateAppearance();
    }
}
