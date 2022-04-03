using Content.Client.HUD;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class ThemedTextureButton : TextureButton , IThemeableUI
{
    public string Path { get; set; }
    public Texture ThemeTexture => Theme.ResolveTexture(Path);
    public UITheme Theme { get; set; }

    public ThemedTextureButton()
    {
        Path = "";
        Theme = UITheme.Default;
    }
    public void UpdateTheme(UITheme newTheme)
    {
        TextureNormal = ThemeTexture;
    }
}
