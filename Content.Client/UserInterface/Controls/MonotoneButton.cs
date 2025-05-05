using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.Label;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A button intended for use with a monotone color palette
/// </summary>
public sealed class MonotoneButton : ContainerButton
{
    /// <summary>
    /// Specifies the color of the label text when the button is pressed.
    /// </summary>
    [ViewVariables]
    public Color AltTextColor { set; get; } = new Color(0.2f, 0.2f, 0.2f);

    /// <summary>
    /// The label that holds the button text.
    /// </summary>
    public Label Label { get; }

    /// <summary>
    /// The text displayed by the button.
    /// </summary>
    [PublicAPI, ViewVariables]
    public string? Text { get => Label.Text; set => Label.Text = value; }

    /// <summary>
    /// How to align the text inside the button.
    /// </summary>
    [PublicAPI, ViewVariables]
    public AlignMode TextAlign { get => Label.Align; set => Label.Align = value; }

    /// <summary>
    /// If true, the button will allow shrinking and clip text
    /// to prevent the text from going outside the bounds of the button.
    /// If false, the minimum size will always fit the contained text.
    /// </summary>
    [PublicAPI, ViewVariables]
    public bool ClipText
    {
        get => Label.ClipText;
        set => Label.ClipText = value;
    }

    public MonotoneButton()
    {
        Label = new Label
        {
            StyleClasses = { StyleClassButton }
        };

        AddChild(Label);
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
