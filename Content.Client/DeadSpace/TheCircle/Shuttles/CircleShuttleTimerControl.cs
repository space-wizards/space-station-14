// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.DeadSpace.TheCircle.Shuttles;

public sealed class CircleShuttleTimerControl : PanelContainer
{
    public readonly Label TimerLabel;

    public CircleShuttleTimerControl()
    {
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#16191FCC"),
            BorderColor = Color.FromHex("#5F9C89"),
            BorderThickness = new Thickness(2),
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10,
            ContentMarginTopOverride = 5,
            ContentMarginBottomOverride = 5,
        };

        TimerLabel = new Label
        {
            VerticalAlignment = VAlignment.Center,
        };
        AddChild(TimerLabel);
    }
}
