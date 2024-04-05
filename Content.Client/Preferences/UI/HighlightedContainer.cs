using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Preferences.UI;

public sealed class HighlightedContainer : PanelContainer
{
    public HighlightedContainer()
    {
        PanelOverride = new StyleBoxFlat()
        {
            BackgroundColor = new Color(47, 47, 53),
            ContentMarginTopOverride = 10,
            ContentMarginBottomOverride = 10,
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10
        };
    }
}
