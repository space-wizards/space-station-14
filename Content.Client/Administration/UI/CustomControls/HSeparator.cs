using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class HSeparator : Control
{
    private static readonly Color SeparatorColor = Color.FromHex("#3D4059");

    // Starlight-start
    private readonly StyleBoxFlat _styleBox;

    public Color Color
    {
        get => _styleBox.BackgroundColor;
        set => _styleBox.BackgroundColor = value;
    }
    // Starlight-end

    public HSeparator(Color color)
    {
        // Starlight-start
        _styleBox = new StyleBoxFlat
        {
            BackgroundColor = color,
            ContentMarginBottomOverride = 2,
            ContentMarginLeftOverride = 2
        };
        // Starlight-end

        AddChild(new PanelContainer { PanelOverride = _styleBox }); // Starlight-edit
    }

    public HSeparator() : this(SeparatorColor) { }
}
