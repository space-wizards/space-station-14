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
        Window.SubscribeToListSelectionChange(RequestMoreInfo);
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

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case BanUsernamePanelEuiMsg.FullUsernameRuleInfoReply r:
                var window = new UsernameBanInfoWindow(r);
                window.OpenCentered();
                break;
        }
    }

    public void RequestMoreInfo(int? ruleId)
    {
        if (ruleId is null)
        {
            return;
        }

        SendMessage(new BanUsernamePanelEuiMsg.GetRuleInfoRequest(ruleId ?? -1));
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
