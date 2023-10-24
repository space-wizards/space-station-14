using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Info.PlaytimeStats;

public sealed class PlaytimeStatsHeader : ContainerButton
{
    public PlaytimeStatsHeader()
    {
        RobustXamlLoader.Load(this);
    }
}
