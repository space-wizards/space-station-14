using System.Linq;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.BanUsernamePanel;

[UsedImplicitly]
public sealed class BanUsernamePanelEui : BaseEui
{
    private BanUsernamePanel Window { get; }
    private readonly SortedDictionary<int, UsernameCacheLine> _usernameRulesCache = new();
    public event Action<List<UsernameCacheLine>>? UpdatedCache;
    public IReadOnlyList<UsernameCacheLine> BanList => _usernameRulesCache.Values.ToList();

    public BanUsernamePanelEui()
    {
        Window = new BanUsernamePanel();
        Window.OnClose += () => SendMessage(new CloseEuiMessage());
        Window.UsernameBanSubmitted += (regexRule, reason, ban, regex) => SendMessage(new BanUsernamePanelEuiMsg.CreateUsernameBanRequest(regexRule, reason, ban, regex));
        Window.SubscribeToListSelectionChange(RequestMoreInfo);
        Window.RefreshButtonPressed += RequestUsernameBans;
        SubscribeList();
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
            case BanUsernamePanelEuiMsg.UsernameRuleUpdate r:
                HandelUsernameRuleUpdate(r);
                break;
        }
    }

    private void HandelUsernameRuleUpdate(BanUsernamePanelEuiMsg.UsernameRuleUpdate update)
    {
        if (!update.Add && update.Id == -1)
        {
            _usernameRulesCache.Clear();
            return;
        }

        if (!update.Add)
        {
            _usernameRulesCache.Remove(update.Id);
            UpdatedCache?.Invoke(_usernameRulesCache.Values.ToList());
            return;
        }

        UsernameCacheLine line = new UsernameCacheLine(update.Expression, update.Id, update.ExtendToBan, update.Regex);
        _usernameRulesCache.Add(update.Id, line);
        if (!update.Silent)
        {
            UpdatedCache?.Invoke(_usernameRulesCache.Values.ToList());
        }
    }

    private void SubscribeList()
    {
        UpdatedCache += Window.ListController.PopulateList;
        UpdatedCache.Invoke(_usernameRulesCache.Values.ToList());
    }

    private void UnsubscribeAll()
    {
        // sandbox hates: .GetInvocationList(), so instead, this will allow GC
        UpdatedCache = null;
    }

    public void RequestUsernameBans()
    {
        SendMessage(new BanUsernamePanelEuiMsg.UsernameRuleRefreshRequest());
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
        RequestUsernameBans();
    }

    public override void Closed()
    {
        UnsubscribeAll();
        base.Closed();
        Window.Close();
    }
}
