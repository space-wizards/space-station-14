using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Managers;

public sealed partial class UsernameRuleManager : IUsernameRuleManager, IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ServerDbEntryManager _entryManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _players = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.username";

    // this could be changed to sorted to implement deltas + removed bool or negative id
    // id : (regex, regexString, message, ban)
    private readonly Dictionary<int, (Regex, string, string, bool)> _cachedUsernameRules = new();

    public async void Initialize()
    {
        _db.SubscribeToNotifications(OnDatabaseNotification);

        // needed for deadmin and readmin
        _admin.OnPermsChanged += OnReAdmin;

        _net.RegisterNetMessage<MsgUsernameBans>();
        _net.RegisterNetMessage<MsgRequestUsernameBans>(OnRequestBans);

        var rules = await _db.GetServerUsernameRulesAsync(false);

        if (rules == null)
        {
            _sawmill.Warning("failed to get rules from database");
            return;
        }

        foreach (var ruleDef in rules)
        {
            if (ruleDef.Id == null)
            {
                _sawmill.Warning("rule had Id of null");
                continue;
            }
            CacheCompiledRegex(ruleDef.Id ?? -1, ruleDef.Expression, ruleDef.Message, ruleDef.ExtendToBan);
        }
    }

    private void OnRequestBans(MsgRequestUsernameBans msg)
    {
        if (!_admin.HasAdminFlag(_players.GetSessionByChannel(msg.MsgChannel), AdminFlags.Ban))
        {
            return;
        }

        _sawmill.Info("Received username ban refresh request");

        SendResetUsernameBan(msg.MsgChannel);
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

    private void CacheCompiledRegex(int id, string expression, string message, bool ban)
    {
        _sawmill.Info($"caching rule {id} {expression}");
        if (_cachedUsernameRules.ContainsKey(id))
        {
            _sawmill.Warning($"caching rule {id} already listed in cache");
            return;
        }

        _cachedUsernameRules[id] = (new Regex(expression, RegexOptions.Compiled), expression, message, ban);
    }

    private void ClearCompiledRegex(int id)
    {
        _cachedUsernameRules.Remove(id);
    }

    public async void CreateUsernameRule(string expression, string message, NetUserId? restrictingAdmin, bool extendToBan = false)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return;
        }

        var finalMessage = message ?? expression;

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;

        var ruleDef = new ServerUsernameRuleDef(
            null,
            DateTimeOffset.Now,
            roundId,
            expression,
            finalMessage,
            restrictingAdmin,
            extendToBan,
            false,
            null,
            null);

        int resultId = await _db.CreateUsernameRuleAsync(ruleDef);

        CacheCompiledRegex(resultId, expression, finalMessage, extendToBan);

        SendAddUsernameBan(resultId, expression, finalMessage, extendToBan);

        var adminName = restrictingAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(restrictingAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        var logMessage = Loc.GetString(
            "server-username-rule-create",
            ("admin", adminName),
            ("expression", expression),
            ("message", finalMessage));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);

        KickMatchingConnectedPlayers(resultId, ruleDef, "new username rule");
    }

    public async Task RemoveUsernameRule(int restrictionId, NetUserId? removingAdmin)
    {
        var rule = await _db.GetServerUsernameRuleAsync(restrictionId);

        if (rule == null)
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
        _chat.SendAdminAlert(logMessage);

        await _db.RemoveServerUsernameRuleAsync(restrictionId, removingAdmin, DateTimeOffset.Now);
    }

    public List<(int, string, string, bool)> GetUsernameRules()
    {
        return [];
    }

    public async Task<(bool, string, bool)> IsUsernameBannedAsync(string username)
    {
        foreach ((Regex rule, _, string message, bool ban) in _cachedUsernameRules.Values)
        {
            if (rule.IsMatch(username))
            {
                return (true, message, ban);
            }
        }

        return (false, "", false);
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

        foreach (var ruleDef in rules)
        {
            if (ruleDef.Id == null)
            {
                continue;
            }

            CacheCompiledRegex(ruleDef.Id ?? -1, ruleDef.Expression, ruleDef.Message, ruleDef.ExtendToBan);
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

        (Regex compiledRule, _, _, _) = _cachedUsernameRules[id];

        // could there be a concurrency issue resulting in null?

        foreach (var player in _playerManager.Sessions)
        {
            if (compiledRule.IsMatch(player.Name))
            {
                KickForUsernameRuleDef(player, def);
                _sawmill.Info($"Kicked player {player.Name} ({player.UserId}) through {source}");
            }
        }
    }

    private void KickForUsernameRuleDef(ICommonSession player, ServerUsernameRuleDef def)
    {
        var message = def.FormatUsernameViolationMessage(_cfg, _localizationManager);
        player.Channel.Disconnect(message);
    }

    private MsgUsernameBans CreateResetMessage()
    {
        List<(bool, bool, int, string, string)> messageContent = new();

        messageContent.EnsureCapacity(_cachedUsernameRules.Count + 1);

        messageContent.Add((false, false, -1, "", ""));

        foreach (var id in _cachedUsernameRules.Keys)
        {
            (_, string expression, string message, bool extendToBan) = _cachedUsernameRules[id];
            messageContent.Add((true, extendToBan, id, expression, message));
        }

        return new MsgUsernameBans()
        {
            UsernameBans = messageContent,
        };
    }

    private void SendResetUsernameBan(INetChannel channel)
    {
        _sawmill.Debug($"Sent username bans reset to connecting admin");
        _net.ServerSendMessage(CreateResetMessage(), channel);
    }

    private void SendResetUsernameBan()
    {
        _sawmill.Debug($"Sent username bans reset to active admins");
        _net.ServerSendToMany(CreateResetMessage(), _admin.ActiveAdmins.Select(a => a.Channel).ToList());
    }

    private void SendAddUsernameBan(int id, string expression, string message, bool extendToBan)
    {
        var usernameBansMsg = new MsgUsernameBans()
        {
            UsernameBans = [(true, extendToBan, id, expression, message)],
        };

        _sawmill.Debug($"sent new username ban {id} to active admins");
        _net.ServerSendToMany(usernameBansMsg, _admin.ActiveAdmins.Select(a => a.Channel).ToList());
    }

    private void SendRemoveUsernameBan(int id)
    {
        var usernameBansMsg = new MsgUsernameBans()
        {
            UsernameBans = [(false, false, id, "", "")],
        };

        _sawmill.Debug($"sent username ban delete {id} to active admins");
        _net.ServerSendToMany(usernameBansMsg, _admin.ActiveAdmins.Select(a => a.Channel).ToList());
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}
