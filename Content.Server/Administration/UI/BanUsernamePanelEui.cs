using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Administration;

using UsernameHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Content.Server.Administration;

public sealed class BanUsernamePanelEui : BaseEui
{
    [Dependency] private readonly IUsernameRuleManager _usernameRuleManager = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IAdminManager _admins = default!;

    private readonly ISawmill _sawmill;

    public BanUsernamePanelEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _log.GetSawmill("admin.username_bans_eui");
        _usernameRuleManager.UpdatedCache += SendSingleRule;
    }

    public override EuiStateBase GetNewState()
    {
        var hasBan = _admins.HasAdminFlag(Player, AdminFlags.Ban);
        var hasHost = _admins.HasAdminFlag(Player, AdminFlags.Host);
        return new BanUsernamePanelEuiState(hasBan, hasHost);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case BanUsernamePanelEuiMsg.CreateUsernameBanRequest r:
                CreateUsernameBan(r.RegexRule, r.Reason, r.Ban, r.Regex);
                break;
            case BanUsernamePanelEuiMsg.GetRuleInfoRequest r:
                SendFullUsernameInfo(r.RuleId);
                break;
            case BanUsernamePanelEuiMsg.UsernameRuleRefreshRequest r:
                SendRefreshMessages();
                break;
        }
    }

    private void SendRefreshMessages()
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            return;
        }

        var data = _usernameRuleManager.BanData;

        SendClearMessage();

        if (data.Count == 0)
        {
            return;
        }

        for (int i = 0; i < data.Count - 1; i++)
        {
            SendSingleRule(data[i], true);
        }

        var ultimate = data[data.Count - 1];
        SendSingleRule(ultimate, false);
    }

    private void SendClearMessage()
    {
        SendMessage(new BanUsernamePanelEuiMsg.UsernameRuleUpdate(
            "",
            -1,
            false,
            false,
            false,
            true
        ));
    }

    private void SendSingleRule(UsernameCacheLine data, bool silent = false)
    {
        SendMessage(new BanUsernamePanelEuiMsg.UsernameRuleUpdate(
            data.Expression,
            data.Id,
            true,
            data.Regex,
            data.ExtendToBan,
            silent
        ));
    }

    private void SendSingleRule(UsernameCacheLineUpdate data)
    {
        SendMessage(new BanUsernamePanelEuiMsg.UsernameRuleUpdate(
            data.Expression,
            data.Id,
            data.Add,
            data.Regex,
            data.ExtendToBan
        ));
    }

    private void CreateUsernameBan(string regexRule, string? reason, bool ban, bool regex)
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            _sawmill.Warning(Loc.GetString("cmd-ban-username-missing-minimum-permissions", ("admin", Player.Name), ("adminId", Player.UserId)));
            return;
        }

        if (regex && !_admins.HasAdminFlag(Player, AdminFlags.Host))
        {
            _sawmill.Warning(Loc.GetString("cmd-ban-username-missing-minimum-permissions-regex", ("admin", Player.Name), ("adminId", Player.UserId), ("expression", regexRule)));
            return;
        }

        if (!regex && !UsernameHelpers.IsNameValid(regexRule, out var _))
        {
            _sawmill.Warning(Loc.GetString("cmd-ban-username-invalid-simple", ("admin", Player.Name), ("adminId", Player.UserId), ("expression", regexRule)));
            return;
        }

        string finalRegexRule = regexRule;

        string? finalMessage;

        if (string.IsNullOrEmpty(reason))
        {
            _sawmill.Warning(Loc.GetString("cmd-ban-username-missing-reason", ("admin", Player.Name), ("adminId", Player.UserId), ("expression", regexRule)));
            if (regex)
            {
                finalMessage = Loc.GetString("ban-username-default-reason-regex");
            }
            else
            {
                finalMessage = Loc.GetString("ban-username-default-reason-simple");
            }
        }
        else
        {
            finalMessage = reason;
        }

        _usernameRuleManager.CreateUsernameRule(regex, finalRegexRule, finalMessage ?? "", Player.UserId, ban);
    }

    private async void SendFullUsernameInfo(int ruleId)
    {
        var banData = await _usernameRuleManager.GetFullBanInfoAsync(ruleId);
        if (banData is null)
        {
            return;
        }
        var message = new BanUsernamePanelEuiMsg.FullUsernameRuleInfoReply(
            banData.CreationTime.UtcDateTime,
            banData.Id ?? -1,
            banData.Regex,
            banData.ExtendToBan,
            banData.Retired,
            banData.RoundId,
            banData.RestrictingAdmin,
            banData.RetiringAdmin,
            banData.RetireTime?.UtcDateTime,
            banData.Expression,
            banData.Message
        );

        SendMessage(message);
    }

    public override void Close()
    {
        base.Close();
        _usernameRuleManager.UpdatedCache -= SendSingleRule;
    }
}
