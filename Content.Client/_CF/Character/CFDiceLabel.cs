using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CF.Character;

/// <summary>
/// Font Awesome solid dice-d20 (U+F6CF) shown beside a rollable sheet name.
/// </summary>
public sealed class CFDiceLabel : Label
{
    public const char Glyph = '\uF6CF';
    private const string FontPath = "/Fonts/_CF/FontAwesome/fa-solid-900.ttf";
    private static Font? _font;

    public CFDiceLabel()
    {
        Text = Glyph.ToString();
        FontColorOverride = Color.White;
        FontOverride = GetFont();
        VerticalAlignment = VAlignment.Center;
        MinWidth = 16;
    }

    private static Font GetFont()
    {
        if (_font != null)
            return _font;

        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>(FontPath), 12);
        return _font;
    }
}
