using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Humanoid;

public sealed class EyeColorPicker : Control
{
    public event Action<Color>? OnEyeColorPicked;
    public Action<bool>? OnGlowingChanged; //starlight

    private readonly ColorSelectorSliders _colorSelectors;
    //starlight start
    private readonly CheckBox _glowCheckBox = new CheckBox()
    {
        Text = Loc.GetString("marking-glowing")
    };
    //starlight end

    public void SetData(Color color, bool isGlowing) //starlight edited function signature
    {
        _glowCheckBox.Pressed = isGlowing; //starlight
        _colorSelectors.Color = color;
    }

    public EyeColorPicker()
    {
        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        AddChild(vBox);

        vBox.AddChild(_colorSelectors = new ColorSelectorSliders());
        _colorSelectors.SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv; // defaults color selector to HSV

        _colorSelectors.OnColorChanged += ColorValueChanged;

        //starlight start
        vBox.AddChild(_glowCheckBox);

        _glowCheckBox.OnToggled += args =>
        {
            OnGlowingChanged?.Invoke(args.Pressed);
        };
        //starlight end
    }

    private void ColorValueChanged(Color newColor)
    {
        OnEyeColorPicked?.Invoke(newColor);
    }
}
