using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Humanoid;

public sealed class EyeColorPicker : Control
{
    public event Action<Color>? OnEyeColorPicked;
    public Action<bool>? OnGlowingChanged;

    private readonly ColorSelectorSliders _colorSelectors;
    private readonly CheckBox _glowCheckBox = new CheckBox(){
        Text = Loc.GetString("marking-glowing")
    };

    public void SetData(Color color, bool isGlowing)
    {
        _glowCheckBox.Pressed = isGlowing;
        _colorSelectors.Color = color;
    }

    public EyeColorPicker()
    {
        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        AddChild(vBox);

        vBox.AddChild(_glowCheckBox);
        vBox.AddChild(_colorSelectors = new ColorSelectorSliders());

        _glowCheckBox.OnToggled += args =>
        {
            OnGlowingChanged?.Invoke(args.Pressed);
        };

        _colorSelectors.OnColorChanged += ColorValueChanged;
    }

    private void ColorValueChanged(Color newColor)
    {
        OnEyeColorPicked?.Invoke(newColor);
    }
}
