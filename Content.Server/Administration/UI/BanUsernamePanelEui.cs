using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using System.Collections.Immutable;
using System.Net;

using UsernameHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Content.Server.Administration;

public sealed class BanUsernamePanelEui : BaseEui
{
    [Dependency] private readonly IUsernameRuleManager _usernameRules = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IAdminManager _admins = default!;

    private readonly ISawmill _sawmill;

    private NetUserId? PlayerId { get; set; }
    private string PlayerName { get; set; } = string.Empty;
    private IPAddress? LastAddress { get; set; }
    private ImmutableArray<byte>? LastHwid { get; set; }
    private const int Ipv4_CIDR = 32;
    private const int Ipv6_CIDR = 64;

    public BanUsernamePanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("admin.username_bans_eui");
    }

    public override EuiStateBase GetNewState()
    {
        var hasBan = _admins.HasAdminFlag(Player, AdminFlags.Ban);
        var hasMassBan = _admins.HasAdminFlag(Player, AdminFlags.MassBan);
        return new BanUsernamePanelEuiState(PlayerName, hasBan, hasMassBan);
    }

    public async void ChangePlayer(string playerNameOrId)
    {
        var located = await _playerLocator.LookupIdByNameOrIdAsync(playerNameOrId);
        ChangePlayer(located?.UserId, located?.Username ?? string.Empty, located?.LastAddress, located?.LastHWId);
    }

    public void ChangePlayer(NetUserId? playerId, string playerName, IPAddress? lastAddress, ImmutableArray<byte>? lastHwid)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        LastAddress = lastAddress;
        LastHwid = lastHwid;
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case BanUsernamePanelEuiStateMsg.CreateUsernameBanRequest r:
                CreateUsernameBan(r.RegexRule, r.Reason, r.Ban, r.Regex);
                break;
            case BanUsernamePanelEuiStateMsg.GetRuleInfoRequest r:
                SendInfo(r.RuleId);
                break;
        }
    }

    private async void CreateUsernameBan(string regexRule, string? reason, bool ban, bool regex)
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Ban)) {
            _sawmill.Warning(Loc.GetString("cmd-ban-username-missing-minimum-permissions", ("admin", Player.Name), ("adminId", Player.UserId)));
            return;
        }

        if (regex && !_admins.HasAdminFlag(Player, AdminFlags.MassBan)) {
            _sawmill.Warning(Loc.GetString("cmd-ban-username-missing-minimum-permissions-regex", ("admin", Player.Name), ("adminId", Player.UserId), ("expression", regexRule)));
            return;
        }

        if (!regex && !UsernameHelpers.IsNameValid(regexRule, out var _)) {
            _sawmill.Warning(Loc.GetString("cmd-ban-username-invalid-simple", ("admin", Player.Name), ("adminId", Player.UserId), ("expression", regexRule)));
            return;
        }

        string finalRegexRule = regexRule;

        if (!regex) {
            finalRegexRule = $"^{finalRegexRule}$";
        }

        string? finalMessage;

        if (string.IsNullOrEmpty(reason)) {
            _sawmill.Warning(Loc.GetString("cmd-ban-username-missing-reason", ("admin", Player.Name), ("adminId", Player.UserId), ("expression", regexRule)));
            if (regex) {
                finalMessage = Loc.GetString("ban-username-default-reason-regex");
            }
            else {
                finalMessage = Loc.GetString("ban-username-default-reason-simple");
            }
        }
        else {
            finalMessage = reason;
        }

        _usernameRules.CreateUsernameRule(finalRegexRule, finalMessage ?? "", Player.UserId, ban);

        Close();
    }

    private void SendInfo(int ruleId)
    {
        return;
    }
}
