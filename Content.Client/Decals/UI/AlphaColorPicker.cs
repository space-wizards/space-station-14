using System.Linq;
using Content.Client.CharacterAppearance;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Decals.UI;

/// <summary>
///     This is just copied from EyeColorPicker btw. I might make it more generic
/// </summary>
public sealed class AlphaColorPicker : Control
{
    public event Action<Color>? OnColorPicked;

    private readonly ColorSlider _colorSliderR;
    private readonly ColorSlider _colorSliderG;
    private readonly ColorSlider _colorSliderB;
    private readonly ColorSlider _colorSliderA;
    private PaletteColorPicker? _picker;

    private Color _lastColor;

    public void SetData(Color color)
    {
        _lastColor = color;

        _colorSliderR.ColorValue = color.RByte;
        _colorSliderG.ColorValue = color.GByte;
        _colorSliderB.ColorValue = color.BByte;
        _colorSliderA.ColorValue = color.AByte;
    }

    public AlphaColorPicker()
    {
        Button pickerOpen;
        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        AddChild(vBox);

        vBox.AddChild(_colorSliderR = new ColorSlider(StyleNano.StyleClassSliderRed));
        vBox.AddChild(_colorSliderG = new ColorSlider(StyleNano.StyleClassSliderGreen));
        vBox.AddChild(_colorSliderB = new ColorSlider(StyleNano.StyleClassSliderBlue));
        vBox.AddChild(_colorSliderA = new ColorSlider(StyleNano.StyleClassSliderWhite));
        vBox.AddChild(pickerOpen = new Button
        {
            Text = "Palette"
        });

        pickerOpen.OnPressed += _ =>
        {
            if (_picker is null)
            {
                _picker = new PaletteColorPicker();
                _picker.OpenToLeft();
                _picker.PaletteList.OnItemSelected += args =>
                {
                    SetData((args.ItemList.GetSelected().First().Metadata as Color?)!.Value);
                    ColorValueChanged();
                };
            }
            else
            {
                if (_picker.IsOpen)
                {
                    _picker.Close();
                }
                else
                {
                    _picker.Open();
                }
            }
        };


        var colorValueChanged = ColorValueChanged;
        _colorSliderR.OnValueChanged += colorValueChanged;
        _colorSliderG.OnValueChanged += colorValueChanged;
        _colorSliderB.OnValueChanged += colorValueChanged;
        _colorSliderA.OnValueChanged += colorValueChanged;
    }

    private void ColorValueChanged()
    {
        var newColor = new Color(
            _colorSliderR.ColorValue,
            _colorSliderG.ColorValue,
            _colorSliderB.ColorValue,
            _colorSliderA.ColorValue
        );

        OnColorPicked?.Invoke(newColor);

        _lastColor = newColor;
    }
}
