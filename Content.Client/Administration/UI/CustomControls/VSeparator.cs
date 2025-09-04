using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class VSeparator : PanelContainer
{
    private static readonly Color SeparatorColor = Color.FromHex("#3D4059");

    private readonly StyleBoxFlat _styleBox;

    public Color Color
    {
        get => _styleBox.BackgroundColor;
        set => _styleBox.BackgroundColor = value;
    }

    public VSeparator(Color color)
    {
        MinSize = new Vector2(2, 5);

        _styleBox = new StyleBoxFlat
        {
            BackgroundColor = color,
        };

        AddChild(new PanelContainer { PanelOverride = _styleBox });
    }

    public VSeparator() : this(SeparatorColor) { }
}
