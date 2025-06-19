using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Humanoid;

public sealed class EyeColorPicker : Control
{
    public event Action<Color>? OnEyeColorPicked;

    private readonly ColorSelectorSliders _colorSelectors;

    private Color _lastColor;

    public void SetData(Color color)
    {
        _lastColor = color;

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
        // this is most certainly not the right way to do this but this was the only way I could get it to find the dropbox reliably.
        if (_colorSelectors.GetChild(0) is BoxContainer rootBox &&
            rootBox.GetChild(0) is BoxContainer headerBox &&
            headerBox.GetChild(0) is OptionButton typeSelector)
        {
            typeSelector.Select((int) ColorSelectorSliders.ColorSelectorType.Hsv);
        }

        _colorSelectors.OnColorChanged += ColorValueChanged;
    }

    private void ColorValueChanged(Color newColor)
    {
        OnEyeColorPicked?.Invoke(newColor);

        _lastColor = newColor;
    }
}
