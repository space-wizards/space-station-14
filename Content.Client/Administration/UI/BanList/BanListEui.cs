using Content.Client.Eui;
using Content.Shared.Administration.BanList;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Client.Administration.UI.BanList;

public sealed class BanListEui : BaseEui
{
    public BanListEui()
    {
        BanWindow = new BanListWindow();
        BanWindow.OnClose += OnClosed;
        BanControl = BanWindow.BanList;
    }

    private void OnClosed()
    {
        SendMessage(new CloseEuiMessage());
    }

    public override void Closed()
    {
        base.Closed();
        BanWindow.Close();
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
