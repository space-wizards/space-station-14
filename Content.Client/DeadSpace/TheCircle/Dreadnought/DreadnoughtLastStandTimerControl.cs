// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.DeadSpace.TheCircle.Dreadnought;

public sealed class DreadnoughtLastStandTimerControl : PanelContainer
{
    public readonly Label TimerLabel;

    public DreadnoughtLastStandTimerControl(Action close)
    {
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#16191FCC"),
            BorderColor = Color.FromHex("#7A1717"),
            BorderThickness = new Thickness(2),
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 6,
            ContentMarginTopOverride = 5,
            ContentMarginBottomOverride = 5,
        };

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };

        TimerLabel = new Label
        {
            VerticalAlignment = VAlignment.Center,
        };

        var closeButton = new Button
        {
            Text = "×",
            ToolTip = Loc.GetString("dreadnought-last-stand-timer-close"),
            MinSize = new Vector2(28, 28),
        };
        closeButton.OnPressed += _ => close();

        row.AddChild(TimerLabel);
        row.AddChild(closeButton);
        AddChild(row);
    }
}
