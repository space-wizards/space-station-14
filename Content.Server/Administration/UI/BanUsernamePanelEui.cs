using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Administration;

using UsernameHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Content.Server.Administration;

public sealed class BanUsernamePanelEui : BaseEui
{
    [Dependency] private readonly IUsernameRuleManager _usernameRules = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IAdminManager _admins = default!;

    private readonly ISawmill _sawmill;

    public BanUsernamePanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("admin.username_bans_eui");
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
                SendInfo(r.RuleId);
                break;
        }
    }

    private async void CreateUsernameBan(string regexRule, string? reason, bool ban, bool regex)
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

        _usernameRules.CreateUsernameRule(regex, finalRegexRule, finalMessage ?? "", Player.UserId, ban);

        Close();
    }

    private void SendInfo(int ruleId)
    {
        return;
    }
}
