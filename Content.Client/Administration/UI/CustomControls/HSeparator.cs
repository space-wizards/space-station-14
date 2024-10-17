using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class HSeparator : Control
{
    private static readonly Color DefaultSeparatorColor = Color.FromHex("#3D4059");

    private Color _separatorColor = DefaultSeparatorColor;
    public Color SeparatorColor
    {
        get => _separatorColor;
        set
        {
            _separatorColor = Color.FromHex(value.ToHex());
            UpdateSeparatorColor();
        }
    }

    private PanelContainer? _panelContainer = null;

    public HSeparator(Color color)
    {
        SeparatorColor = color;
        Initialize();
    }

    public HSeparator() : this(DefaultSeparatorColor) { }

    private void Initialize()
    {
        _panelContainer = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = SeparatorColor,
                ContentMarginBottomOverride = 2,
                ContentMarginLeftOverride = 2
            }
        };
        AddChild(_panelContainer);
    }

    private void UpdateSeparatorColor()
    {
        if (_panelContainer?.PanelOverride is StyleBoxFlat styleBox)
        {
            styleBox.BackgroundColor = SeparatorColor;
        }
    }
}
