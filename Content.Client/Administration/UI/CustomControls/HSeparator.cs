using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class HSeparator : Control
{
    public Color SeparatorColor { get; set; }

    private static readonly Color DefaultSeparatorColor = Color.FromHex("#3D4059");

    public const string StylePropertyColor = "color";

    public HSeparator(Color color)
    {
        SeparatorColor = color;
        if (TryGetStyleProperty<Color>(StylePropertyColor, out var bgColor))
        {
            color = bgColor;
        }

        AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = color,
                    ContentMarginBottomOverride = 2,
                    ContentMarginLeftOverride = 2,
                },
            }
        );
    }

    public HSeparator() : this(DefaultSeparatorColor) { }
}
