using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.BanUsernamePanel;

[UsedImplicitly]
public sealed class BanUsernamePanelEui : BaseEui
{
    private BanUsernamePanel BanUsernamePanel { get; }

    public BanUsernamePanelEui()
    {
        BanUsernamePanel = new BanUsernamePanel();
        BanUsernamePanel.OnClose += () => SendMessage(new CloseEuiMessage());
        BanUsernamePanel.UsernameBanSubmitted += (regexRule, reason, ban, regex) => SendMessage(new BanUsernamePanelEuiStateMsg.CreateUsernameBanRequest(regexRule, reason, ban, regex));
        BanUsernamePanel.RuleUpdate += (ruleId) => SendMessage(new BanUsernamePanelEuiStateMsg.GetRuleInfoRequest(ruleId));
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not BanUsernamePanelEuiState s)
        {
            return;
        }

        // BanUsernamePanel.UpdateBanFlag(s.HasBan);
        // BanUsernamePanel.UpdatePlayerData(s.PlayerName);
    }

    public override void Opened()
    {
        BanUsernamePanel.OpenCentered();
    }

    public override void Closed()
    {
        BanUsernamePanel.Close();
        BanUsernamePanel.Dispose();
    }
}
