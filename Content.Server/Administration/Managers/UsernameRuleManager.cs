using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Players.RateLimiting;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Managers;

public sealed partial class UsernameRuleManager : IUsernameRuleManager, IPostInjectInit
{
    private readonly record struct ServerUsernameCacheLine(Regex? CompiledRule, string Expression, string Message, bool ExtendToBan)
    {
        public bool Regex => CompiledRule is not null;
    }

    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ServerDbEntryManager _entryManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly IEntitySystemManager _systemsManager = default!;
    [Dependency] private readonly PlayerRateLimitManager _rateLimitManager = default!;

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.username";
    private const string RateLimitKey = "UsernameRule";

    // this could be changed to sorted to implement deltas + (deltas would require a removed bool or negative id)
    // id : (regex, regexString, message, ban)
    private readonly Dictionary<int, ServerUsernameCacheLine> _cachedUsernameRules = new();

    // cache for non-regex (exact match) rules points to cache for all rules
    private readonly Dictionary<string, int> _cachedUsernames = new();

    public async void Initialize()
    {
        _db.SubscribeToNotifications<UsernameRuleNotification>(ProcessUsernameRuleNotification, UsernameRuleNotificationChannel);

        // needed for deadmin and readmin
        _adminManager.OnPermsChanged += OnReAdmin;

        RegisterRateLimits();

        _netManager.RegisterNetMessage<MsgUsernameBan>();
        _netManager.RegisterNetMessage<MsgRequestUsernameBans>(OnRequestBans);

        var rules = await _db.GetServerUsernameRulesAsync(false);

        foreach (var ruleDef in rules)
        {
            DebugTools.AssertNotNull(ruleDef.Id);
            CacheCompiledRegex(ruleDef.Id ?? -1, ruleDef.Regex, ruleDef.Expression, ruleDef.Message, ruleDef.ExtendToBan);
        }
    }

    private void OnRequestBans(MsgRequestUsernameBans msg)
    {
        if (!_playerManager.TryGetSessionById(msg.MsgChannel.UserId, out var player))
        {
            return;
        }

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Ban))
        {
            return;
        }

        if (RequestRateLimited(player))
        {
            return;
        }

        _sawmill.Debug(Loc.GetString("Received username ban refresh request"));
        SendResetUsernameBan(msg.MsgChannel);
    }

    private void RegisterRateLimits()
    {
        _rateLimitManager.Register(
            RateLimitKey,
            new RateLimitRegistration(
                CCVars.RequestUsernameRestrictionPeriod,
                CCVars.RequestUsernameRestrictionMaxCount,
                null, null, null
        ));
    }

    private bool RequestRateLimited(ICommonSession player)
    {
        return _rateLimitManager.CountAction(player, RateLimitKey) == RateLimitStatus.Blocked;
    }

    public async Task<ServerUsernameRuleDef?> GetFullBanInfoAsync(int banId)
    {
        return await _db.GetServerUsernameRuleAsync(banId);
    }

    private void OnReAdmin(AdminPermsChangedEventArgs args)
    {
        if (args is null || args?.Flags == null || (args?.Flags & AdminFlags.Ban) == AdminFlags.Ban)
        {
            return;
        }

        if (args is not null)
        {
            SendResetUsernameBan(args.Player.Channel);
        }
    }

    private void CacheCompiledRegex(int id, bool regex, string expression, string message, bool ban)
    {

        if (_cachedUsernameRules.ContainsKey(id))
        {
            _sawmill.Warning($"canceled caching attempt rule {id} already listed in cache");
            return;
        }

        _sawmill.Info($"caching rule {id} {expression}");

        var compiledRegex = regex ? new Regex(expression, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace) : null;

        if (!regex)
        {
            _cachedUsernames.Add(expression, id);
        }

        _cachedUsernameRules[id] = new ServerUsernameCacheLine(compiledRegex, expression, message, ban);
    }

    private void ClearCompiledRegex(int id)
    {
        var expression = _cachedUsernameRules[id].Expression;
        _cachedUsernames.Remove(expression);
        _cachedUsernameRules.Remove(id);
    }

    public async void CreateUsernameRule(bool regex, string expression, string message, NetUserId? restrictingAdmin, bool extendToBan = false)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return;
        }

        var finalMessage = message ?? expression;

        _systemsManager.TryGetEntitySystem<GameTicker>(out var ticker);

        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;

        var ruleDef = new ServerUsernameRuleDef(
            null,
            DateTimeOffset.Now,
            roundId,
            regex,
            expression,
            finalMessage,
            restrictingAdmin,
            extendToBan,
            false,
            null,
            null);

        int resultId = await _db.CreateUsernameRuleAsync(ruleDef);

        CacheCompiledRegex(resultId, regex, expression, finalMessage, extendToBan);

        SendAddUsernameBan(resultId, regex, expression, finalMessage, extendToBan);

        var adminName = restrictingAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(restrictingAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        var logMessage = Loc.GetString(
            "server-username-rule-create",
            ("admin", adminName),
            ("expression", expression),
            ("message", finalMessage));

        _sawmill.Info(logMessage);
        _chatManager.SendAdminAlert(logMessage);

        KickMatchingConnectedPlayers(resultId, ruleDef, "new username rule");
    }

    public async Task RemoveUsernameRule(int restrictionId, NetUserId? removingAdmin)
    {
        _playerManager.TryGetSessionById(removingAdmin, out var player);

        // ensure that user has ban
        if (removingAdmin is not null
            && player is not null
            && !_adminManager.HasAdminFlag(player, AdminFlags.Ban)
        )
        {
            return;
        }

        var rule = await _db.GetServerUsernameRuleAsync(restrictionId);

        if (rule is null)
        {
            return;
        }

        // if the rule is regex ensure that user is host
        if (rule.Regex
            && removingAdmin is not null // fairly certain null indicates system
            && player is not null
            && !_adminManager.HasAdminFlag(player, AdminFlags.Host)
        )
        {
            return;
        }

        ClearCompiledRegex(restrictionId);
        SendRemoveUsernameBan(restrictionId);

        var adminName = removingAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(removingAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        var logMessage = Loc.GetString(
            "server-username-rule-remove",
            ("admin", adminName),
            ("expression", rule.Expression),
            ("message", rule.Message));

        _sawmill.Info(logMessage);
        _chatManager.SendAdminAlert(logMessage);

        await _db.RemoveServerUsernameRuleAsync(restrictionId, removingAdmin, DateTimeOffset.Now);
    }

    public async Task<UsernameBanStatus> IsUsernameBannedAsync(string username)
    {
        var whitelist = await _db.CheckUsernameWhitelistAsync(username);
        if (whitelist)
        {
            return new UsernameBanStatus("", false, false);
        }

        // check simple rules
        if (_cachedUsernames.ContainsKey(username))
        {
            var rule = _cachedUsernameRules[_cachedUsernames[username]];
            var fullMessage = ServerUsernameRuleDef.FormatUsernameViolationMessage(_cfgManager, _localizationManager, rule.Message);
            return new UsernameBanStatus(fullMessage, rule.ExtendToBan, true);
        }

        // check regex rules
        foreach (var rule in _cachedUsernameRules.Values)
        {
            if (rule.CompiledRule?.IsMatch(username) ?? false)
            {
                var fullMessage = ServerUsernameRuleDef.FormatUsernameViolationMessage(_cfgManager, _localizationManager, rule.Message);
                return new UsernameBanStatus(fullMessage, rule.ExtendToBan, true);
            }
        }

        return new UsernameBanStatus("", false, false);
    }

    public async void Restart()
    {
        var rules = await _db.GetServerUsernameRulesAsync(false);

        if (rules == null)
        {
            _sawmill.Warning("service restart failed");
            return;
        }

        _cachedUsernameRules.Clear();
        _cachedUsernames.Clear();

        foreach (var ruleDef in rules)
        {
            if (ruleDef.Id == null)
            {
                continue;
            }

            CacheCompiledRegex(ruleDef.Id ?? -1, ruleDef.Regex, ruleDef.Expression, ruleDef.Message, ruleDef.ExtendToBan);
            KickMatchingConnectedPlayers(ruleDef.Id ?? -1, ruleDef, "username rule service restart");
        }

        SendResetUsernameBan();
    }

    private void KickMatchingConnectedPlayers(int id, ServerUsernameRuleDef def, string source)
    {
        if (!_cachedUsernameRules.ContainsKey(id))
        {
            return;
        }

        (Regex? compiledRule, string expression, _, _) = _cachedUsernameRules[id];

        if (compiledRule == null)
        {
            _playerManager.TryGetSessionByUsername(expression, out var player);
            if (player != null)
            {
                KickForUsernameRuleDef(player, def);
            }
            return;
        }

        foreach (var player in _playerManager.Sessions)
        {
            if (compiledRule?.IsMatch(player.Name) ?? false)
            {
                KickForUsernameRuleDef(player, def);
                _sawmill.Info($"Kicked player {player.Name} ({player.UserId}) through {source}");
            }
        }
    }

    private void KickForUsernameRuleDef(ICommonSession player, ServerUsernameRuleDef def)
    {
        var message = def.FormatUsernameViolationMessage(_cfgManager, _localizationManager);
        player.Channel.Disconnect(message);
    }

    private IEnumerable<MsgUsernameBan> CreateResetMessages()
    {
        List<MsgUsernameBanContent> messageContent = new();

        messageContent.EnsureCapacity(_cachedUsernameRules.Count + 1);

        messageContent.Add(new(-1, false, false, false, "empty"));

        foreach (var id in _cachedUsernameRules.Keys)
        {
            (Regex? regex, string expression, string message, bool extendToBan) = _cachedUsernameRules[id];

            messageContent.Add(new(id, true, regex != null, extendToBan, expression));
        }

        return messageContent.Select(static mc =>
        {
            return new MsgUsernameBan()
            {
                UsernameBan = mc
            };
        });
    }

    private void SendResetUsernameBan(INetChannel channel)
    {
        var resetMessages = CreateResetMessages();
        foreach (var msg in resetMessages)
        {
            _netManager.ServerSendMessage(msg, channel);
        }
    }

    private void SendResetUsernameBan()
    {
        _sawmill.Debug("Sent username bans reset to active admins");
        var adminChannels = _adminManager.ActiveAdmins.Select(a => a.Channel).ToList();
        var resetMessages = CreateResetMessages();
        foreach (var msg in resetMessages)
        {
            _netManager.ServerSendToMany(msg, adminChannels);
        }
    }

    private void SendAddUsernameBan(int id, bool regex, string expression, string message, bool extendToBan)
    {
        var usernameBanMsg = new MsgUsernameBan()
        {
            UsernameBan = new(id, true, regex, extendToBan, expression),
        };

        _sawmill.Debug($"sent new username ban {id} to active admins");
        _netManager.ServerSendToMany(usernameBanMsg, _adminManager.ActiveAdmins.Select(a => a.Channel).ToList());
    }

    private void SendRemoveUsernameBan(int id)
    {
        var usernameBanMsg = new MsgUsernameBan()
        {
            UsernameBan = new(id, false, false, false, "empty"),
        };

        _sawmill.Debug($"sent username ban delete {id} to active admins");
        _netManager.ServerSendToMany(usernameBanMsg, _adminManager.ActiveAdmins.Select(a => a.Channel).ToList());
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }

    public async Task WhitelistAddUsernameAsync(string username)
    {
        await _db.AddUsernameWhitelistAsync(username);
        _sawmill.Debug($"sent create username whitelist for {username} to db");
    }

    public async Task<bool> WhitelistRemoveUsernameAsync(string username)
    {
        bool present = await _db.CheckUsernameWhitelistAsync(username);
        if (present)
        {
            await _db.RemoveUsernameWhitelistAsync(username);
        }
        _sawmill.Debug($"sent delete username whitelist for {username} to db");
        return present;
    }
}
