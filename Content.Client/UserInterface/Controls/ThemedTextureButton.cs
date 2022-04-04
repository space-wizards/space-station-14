using Content.Client.HUD;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class ThemedTextureButton : TextureButton
{
    public string Path { get; set; }
    public Texture ThemeTexture => Theme.ResolveTexture(Path);

    public ThemedTextureButton()
    {
        Path = "";
    }

    protected override void OnThemeUpdated()
    {
        TextureNormal = ThemeTexture;
    }
}
