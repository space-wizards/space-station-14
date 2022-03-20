using Content.Client.HUD;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class ThemedTextureButton : TextureButton , IHasHudTheme
{
    public string Path { get; set; }
    public Texture ThemeTexture => Theme.ResolveTexture(Path);
    public HudTheme Theme { get; set; }

    public ThemedTextureButton()
    {
        Path = "";
        Theme = HudThemes.DefaultTheme;
    }
    public void UpdateTheme(HudTheme newTheme)
    {
        TextureNormal = ThemeTexture;
    }
}
