using Robust.Client.UserInterface;

namespace Content.Client.UserInterface;

public static class Theme
{
    public static UITheme Default => UITheme.Default;
    public static readonly UITheme Modern = new UITheme("Modern");
    public static readonly UITheme Classic = new UITheme("Classic");
}
