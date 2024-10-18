using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.BanUsernamePanel;

[UsedImplicitly]
public sealed class BanUsernamePanelEui : BaseEui
{
    private BanUsernamePanel Window { get; }

    public BanUsernamePanelEui()
    {
        Window = new BanUsernamePanel();
        Window.OnClose += () => SendMessage(new CloseEuiMessage());
        Window.UsernameBanSubmitted += (regexRule, reason, ban, regex) => SendMessage(new BanUsernamePanelEuiMsg.CreateUsernameBanRequest(regexRule, reason, ban, regex));
        Window.RuleUpdate += (ruleId) => SendMessage(new BanUsernamePanelEuiMsg.GetRuleInfoRequest(ruleId));
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not BanUsernamePanelEuiState s)
        {
            return;
        }

        Window.UpdateBanFlag(s.HasBan);
        Window.UpdateMassBanFlag(s.HasHost);
    }

    public override void Opened()
    {
        base.Opened();
        Window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        Window.Close();
    }
}
