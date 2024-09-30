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
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.username";

    private readonly Dictionary<string, Regex> _cachedUsernameRules = new();

    public async void Initialize()
    {
        _db.SubscribeToNotifications(OnDatabaseNotification);

        var usernameRules = await GetUsernameRulesAsync();

        if (usernameRules == null) {
            return;
        }

        foreach (var rule in usernameRules) {
            CacheCompiledRegex(rule);
        }
    }

    private void CacheCompiledRegex(string ruleText)
    {
        // adding a rule that exists could cause major issue with the caching mechanism on remove
        if ( _cachedUsernameRules.ContainsKey(ruleText)) {
            return;
        }
        _cachedUsernameRules[ruleText] = new Regex(ruleText, RegexOptions.Compiled);
    }

    private void ClearCompiledRegex(string ruleText)
    {
        // checking the database, counting rule occurrences, or ensuring active rules are unique could be required to prevent shenanigans
        _cachedUsernameRules.Remove(ruleText);
    }

    public async void CreateUsernameRule(string? expression, string? message, NetUserId? restrictingAdmin, bool extendToBan = false)
    {
        if (string.IsNullOrEmpty(expression)) {
            return;
        }

        var finalMessage = message == null? expression : message;

        var ruleDef = new ServerUsernameRuleDef(
            null,
            DateTimeOffset.Now,
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

    public async void Restart()
    {
        var usernameRules = await _db.GetServerUsernameRulesAsync(false);

        if (usernameRules == null) {
            return; // we know nothing keep current state
        }

        var expressions = usernameRules.Select(r => r.Expression).ToHashSet();

        // add missed rules
        foreach (var ruleDef in usernameRules) {
            KickMatchingConnectedPlayers(ruleDef, "username rule service restart");
        }

        // remove rules
        foreach (var ruleExpression in _cachedUsernameRules.Keys) {
            if (!expressions.Contains(ruleExpression)) {
                ClearCompiledRegex(ruleExpression);
            }
        }
    }

    private void KickMatchingConnectedPlayers(ServerUsernameRuleDef def, string source)
    {
        CacheCompiledRegex(def.Expression);

        Regex CompiledRule = _cachedUsernameRules[def.Expression];

        // could there be a concurrency issue resulting in null?

        foreach (var player in _playerManager.Sessions)
        {
            if (CompiledRule.Equals(player.Name))
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
