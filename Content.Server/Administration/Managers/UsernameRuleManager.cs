using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.username";

    // expression: (regex, message, count) (+ ban?)
    private readonly Dictionary<string, (Regex, string, int)> _cachedUsernameRules = new();

    public async void Initialize()
    {
        _db.SubscribeToNotifications(OnDatabaseNotification);

        var rules = await _db.GetServerUsernameRulesAsync(false);

        if (rules == null) {
            return;
        }

        foreach (var ruleDef in rules) {
            CacheCompiledRegex(ruleDef.Expression, ruleDef.Message);
        }
    }

    private void CacheCompiledRegex(string expression, string message)
    {
        if (_cachedUsernameRules.ContainsKey(expression)) {
            (Regex rule, string oldMessage, int count) = _cachedUsernameRules[expression];
            _cachedUsernameRules[expression] = (rule, message, count + 1);
            return;
        }

        _cachedUsernameRules[expression] = (new Regex(expression, RegexOptions.Compiled), message, 1);
    }

    private void ClearCompiledRegex(string expression)
    {
        if (!_cachedUsernameRules.ContainsKey(expression)) {
            return;
        }

        (Regex rule, string message, int count) = _cachedUsernameRules[expression];

        if (count <= 1) {
            _cachedUsernameRules.Remove(expression);
            return;
        }

        _cachedUsernameRules[expression] = (rule, message, count - 1);
    }

    public async void CreateUsernameRule(string expression, string message, NetUserId? restrictingAdmin, bool extendToBan = false)
    {
        if (string.IsNullOrEmpty(expression)) {
            return;
        }

        var finalMessage = message ?? expression;

        CacheCompiledRegex(expression, finalMessage);

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

        await _db.CreateUsernameRuleAsync(ruleDef);

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

        KickMatchingConnectedPlayers(ruleDef, "new username rule");
    }

    public async Task RemoveUsernameRule(int restrictionId, NetUserId? removingAdmin)
    {
        var rule = await _db.GetServerUsernameRuleAsync(restrictionId);

        if (rule == null) {
            return;
        }

        ClearCompiledRegex(rule.Expression);

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

    public async Task<HashSet<string>?> GetUsernameRulesAsync()
    {
        var rules = await _db.GetServerUsernameRulesAsync(false);

        if (rules == null) {
            return null;
        }

        return rules.Select(r => r.Expression).ToHashSet();
    }

    // matches first only
    public async Task<string?> IsUsernameBannedAsync(string username)
    {
        foreach((Regex rule, string message, int count) in _cachedUsernameRules.Values) {
            if (count > 0 && rule.IsMatch(username)) {
                return message;
            }
        }

        return null;
    }

    public async void Restart()
    {
        _cachedUsernameRules.Clear();

        var rules = await _db.GetServerUsernameRulesAsync(false);

        if (rules == null) {
            return;
        }

        foreach (var ruleDef in rules) {
            CacheCompiledRegex(ruleDef.Expression, ruleDef.Message);
            KickMatchingConnectedPlayers(ruleDef, "username rule service restart");
        }
    }

    private void KickMatchingConnectedPlayers(ServerUsernameRuleDef def, string source)
    {
        if (!_cachedUsernameRules.ContainsKey(def.Expression)) {
            return;
        }

        (Regex CompiledRule, _, _) = _cachedUsernameRules[def.Expression];

        // could there be a concurrency issue resulting in null?

        foreach (var player in _playerManager.Sessions) {
            if (CompiledRule.IsMatch(player.Name))
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

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}
