using Content.Client.UserInterface;
using Robust.Client.UserInterface;

namespace Content.Client.HUD;

public static class HudThemes
{
    public static HudTheme DefaultTheme => Default;
    public static readonly HudTheme Default = new HudTheme("Default");
    public static readonly HudTheme Modern = new HudTheme("Modern");
    public static readonly HudTheme Classic = new HudTheme("Classic");
}
