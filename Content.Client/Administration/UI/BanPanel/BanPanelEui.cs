using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.BanPanel;

[UsedImplicitly]
public sealed class BanPanelEui : BaseEui
{
    private BanPanel BanPanel { get; }

    public BanPanelEui()
    {
        BanPanel = new BanPanel();
        BanPanel.OnClose += () => SendMessage(new CloseEuiMessage());
        BanPanel.BanSubmitted += ban => SendMessage(new BanPanelEuiStateMsg.CreateBanRequest(ban));
        BanPanel.PlayerChanged += player => SendMessage(new BanPanelEuiStateMsg.GetPlayerInfoRequest(player));
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not BanPanelEuiState s)
        {
            return;
        }

        BanPanel.UpdateBanFlag(s.HasBan);
        BanPanel.UpdatePlayerData(s.PlayerName);
    }

    public override void Opened()
    {
        BanPanel.OpenCentered();
    }

    public override void Closed()
    {
        BanPanel.Close();
        BanPanel.Dispose();
    }
}
