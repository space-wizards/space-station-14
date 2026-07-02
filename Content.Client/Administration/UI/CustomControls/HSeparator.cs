using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class HSeparator : Control
{
    public const string StylePropertyColor = "color";

    private static readonly Color DefaultSeparatorColor = Color.FromHex("#3D4059");

    private readonly PanelContainer _panelContainer;

    public HSeparator(Color color)
    {
        if (TryGetStyleProperty<Color>(StylePropertyColor, out var bgColor))
        {
            color = bgColor;
        }

        _panelContainer = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = color,
                ContentMarginBottomOverride = 2,
                ContentMarginLeftOverride = 2,
            },
        };
        AddChild(_panelContainer);
        SeparatorColor = color;
    }

    public HSeparator() : this(DefaultSeparatorColor) { }

    public Color SeparatorColor
    {
        get
        {
            if(_panelContainer?.PanelOverride is StyleBoxFlat styleBox)
            {
                return styleBox.BackgroundColor;
            }

            return DefaultSeparatorColor;
        }
        set
        {
            if (_panelContainer?.PanelOverride is StyleBoxFlat styleBox)
            {
                styleBox.BackgroundColor = value;
            }
        }
    }
}
