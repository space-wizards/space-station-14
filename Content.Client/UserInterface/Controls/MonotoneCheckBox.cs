using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A check box intended for use with a monotone color palette
/// </summary>
public sealed class MonotoneCheckBox : CheckBox
{
    public new const string StyleClassCheckBox = "monotoneCheckBox";
    public new const string StyleClassCheckBoxChecked = "monotoneCheckBoxChecked";

    public MonotoneCheckBox()
    {
        TextureRect.RemoveStyleClass(CheckBox.StyleClassCheckBox);
        TextureRect.AddStyleClass(StyleClassCheckBox);
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();

        // Update appearance
        if (Pressed)
            TextureRect.AddStyleClass(StyleClassCheckBoxChecked);
        else
            TextureRect.RemoveStyleClass(StyleClassCheckBoxChecked);

        // Appearance modulations
        Modulate = Disabled ? Color.Gray : Color.White;
    }
}
