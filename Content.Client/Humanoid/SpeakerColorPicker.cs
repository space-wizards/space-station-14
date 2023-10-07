using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Content.Shared.Ganimed.SponsorManager;
using Robust.Client.Player;
using Content.Client.IoC;
using Robust.Shared.IoC;

namespace Content.Client.Humanoid;

public sealed class SpeakerColorPicker : Control
{
	
	public event Action<Color>? OnSpeakerColorPicked;

    private readonly ColorSelectorSliders _colorSelectors;

    private Color _lastColor;

    public void SetData(Color color)
    {
        _lastColor = color;

        _colorSelectors.Color = color;
    }

    public SpeakerColorPicker()
    {
        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        AddChild(vBox);

        vBox.AddChild(_colorSelectors = new ColorSelectorSliders());
		
		_colorSelectors.OnColorChanged += ColorValueChanged;
		
    }

    private void ColorValueChanged(Color newColor)
    {
		
		OnSpeakerColorPicked?.Invoke(newColor);

        _lastColor = newColor;
    }
}
