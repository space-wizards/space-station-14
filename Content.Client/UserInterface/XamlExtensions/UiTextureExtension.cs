using Content.Client.HUD;
using JetBrains.Annotations;

namespace Content.Client.UserInterface.XamlExtensions;

[PublicAPI]
public sealed class UiTexExtension
{
    public string Path { get; }
    public HudTheme Theme { get; }

    public UiTexExtension( string path)
    {
        Path = path;
        Theme = HudThemes.DefaultTheme;
    }

    public UiTexExtension(HudTheme theme, string path)
    {
        Path = path;
        Theme = theme;
    }

    public object ProvideValue()
    {
        return Theme.ResolveTexture(Path);
    }
}
