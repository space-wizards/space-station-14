using Content.Client.Eui;
using Content.Shared.Administration.BanList;
using Content.Shared.Eui;

namespace Content.Client.Administration.UI.BanList;

public sealed class BanListEui : BaseEui
{
    public BanListEui()
    {
        BanWindow = new BanListWindow();
        BanControl = BanWindow.BanList;
    }

    private BanListWindow BanWindow { get; }

    private BanListControl BanControl { get; }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not BanListEuiState s)
            return;

        BanWindow.SetTitlePlayer(s.BanListPlayerName);

        s.Bans.Sort((a, b) => a.BanTime.CompareTo(b.BanTime));
        BanControl.SetBans(s.Bans);
    }

    public override void Opened()
    {
        BanWindow.OpenCentered();
    }
}
