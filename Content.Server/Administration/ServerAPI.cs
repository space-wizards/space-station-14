using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Maps;
using Content.Server.RoundEnd;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Prototypes;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration;

public sealed class ServerApi : EntitySystem // should probably not be an entity system
{
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!; // Players
    [Dependency] private readonly ISharedAdminManager _adminManager = default!; // Admins
    [Dependency] private readonly GameTicker _gameTicker = default!; // Round ID and stuff
    [Dependency] private readonly IGameMapManager _gameMapManager = default!; // Map name
    [Dependency] private readonly AdminSystem _adminSystem = default!; // Panic bunker
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!; // Round API
    [Dependency] private readonly IServerNetManager _netManager = default!; // Kick
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Game rules

    [Dependency] private readonly IComponentFactory _componentFactory = default!; // Needed to circumvent the "IoC has no context on this thread" error until I figure out how to do it properly

    private string token = default!;
    private ISawmill _sawmill = default!;
    private string _motd = default!;

    protected override void PostInject()
    {
        base.PostInject();
        // Get
        _statusHost.AddHandler(InfoHandler);
        _statusHost.AddHandler(GetGameRules);
        _statusHost.AddHandler(GetForcePresets);

        // Post
        _statusHost.AddHandler(ActionRoundStatus);
        _statusHost.AddHandler(ActionKick);
        _statusHost.AddHandler(ActionAddGameRule);
        _statusHost.AddHandler(ActionEndGameRule);
        _statusHost.AddHandler(ActionForcePreset);
        _statusHost.AddHandler(ActionForceMotd);
        _statusHost.AddHandler(ActionPanicPunker);

        _config.OnValueChanged(CCVars.AdminApiToken, UpdateToken, true);
        _config.OnValueChanged(CCVars.MOTD, UpdateMotd, true);

        _sawmill = Logger.GetSawmill("serverApi");
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _config.UnsubValueChanged(CCVars.AdminApiToken, UpdateToken);
        _config.UnsubValueChanged(CCVars.MOTD, UpdateMotd);
    }

    private void UpdateToken(string token)
    {
        this.token = token;
    }

    private void UpdateMotd(string motd)
    {
        _motd = motd;
    }


#region Actions

    /// <summary>
    ///     Changes the panic bunker settings.
    /// </summary>
    private async Task<bool> ActionPanicPunker(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/panic_bunker")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var actionSupplied = context.RequestHeaders.TryGetValue("Action", out var action);
        if (!actionSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var valueSupplied = context.RequestHeaders.TryGetValue("Value", out var value);
        if (!valueSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        switch (action) // TODO: This looks bad, there has to be a better way to do this.
        {
            case "enabled":
                if (!bool.TryParse(value.ToString(), out var enabled))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _config.SetCVar(CCVars.PanicBunkerEnabled, enabled);
                break;
            case "disable_with_admins":
                if (!bool.TryParse(value.ToString(), out var disableWithAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _config.SetCVar(CCVars.PanicBunkerDisableWithAdmins, disableWithAdmins);
                break;

            case "enable_without_admins":
                if (!bool.TryParse(value.ToString(), out var enableWithoutAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _config.SetCVar(CCVars.PanicBunkerEnableWithoutAdmins, enableWithoutAdmins);
                break;
            case "count_deadminned_admins":
                if (!bool.TryParse(value.ToString(), out var countDeadminnedAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _config.SetCVar(CCVars.PanicBunkerCountDeadminnedAdmins, countDeadminnedAdmins);
                break;
            case "show_reason":
                if (!bool.TryParse(value.ToString(), out var showReason))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _config.SetCVar(CCVars.PanicBunkerShowReason, showReason);
                break;

            case "min_account_age_hours":
                if (!int.TryParse(value.ToString(), out var minAccountAgeHours))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _config.SetCVar(CCVars.PanicBunkerMinAccountAge, minAccountAgeHours * 60);
                break;
            case "min_overall_hours":
                if (!int.TryParse(value.ToString(), out var minOverallHours))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _config.SetCVar(CCVars.PanicBunkerMinOverallHours, minOverallHours * 60);
                break;
        }

        await context.RespondAsync("Success", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Sets the current MOTD.
    /// </summary>
    private async Task<bool> ActionForceMotd(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/set_motd")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var motdSupplied = context.RequestHeaders.TryGetValue("MOTD", out var motd);
        if (!motdSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        _config.SetCVar(CCVars.MOTD, motd.ToString()); // A hook in the MOTD system sends the changes to each client
        await context.RespondAsync("Success", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Forces the next preset-
    /// </summary>
    private async Task<bool> ActionForcePreset(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/force_preset")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            await context.RespondAsync("This can only be executed while the game is in the pre-round lobby.", HttpStatusCode.BadRequest);
            return true;
        }

        var presetSupplied = context.RequestHeaders.TryGetValue("PresetId", out var preset);
        if (!presetSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        if (!_gameTicker.TryFindGamePreset(preset.ToString(), out var type))
        {
            await context.RespondAsync($"No preset exists with name {preset}.", HttpStatusCode.NotFound);
            return true;
        }

        _gameTicker.SetGamePreset(type);
        _sawmill.Info($"Forced the game to start with preset {preset}.");
        await context.RespondAsync("Success", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Ends an active game rule.
    /// </summary>
    private async Task<bool> ActionEndGameRule(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/end_game_rule")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var gameRuleSupplied = context.RequestHeaders.TryGetValue("GameRuleId", out var gameRule);
        if (!gameRuleSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var gameRuleEntity = _gameTicker
            .GetActiveGameRules()
            .FirstOrNull(rule => MetaData(rule).EntityPrototype?.ID == gameRule.ToString());

        if (gameRuleEntity == null) // Game rule not found
        {
            await context.RespondAsync("Gamerule not found or not active",HttpStatusCode.NotFound);
            return true;
        }

        _gameTicker.EndGameRule((EntityUid) gameRuleEntity);
        await context.RespondAsync($"Ended game rule {gameRuleEntity}", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Adds a game rule to the current round.
    /// </summary>
    private async Task<bool> ActionAddGameRule(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/add_game_rule")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var gameRuleSupplied = context.RequestHeaders.TryGetValue("GameRuleId", out var gameRule);
        if (!gameRuleSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var ruleEntity = _gameTicker.AddGameRule(gameRule.ToString());
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
        {
            _gameTicker.StartGameRule(ruleEntity);
        }

        await context.RespondAsync($"Added game rule {ruleEntity}", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Kicks a player.
    /// </summary>
    private async Task<bool> ActionKick(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/kick")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var playerSupplied = context.RequestHeaders.TryGetValue("Username", out var username);
        if (!playerSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var found = _playerManager.TryGetSessionByUsername(username.ToString(), out var session);
        if (!found)
        {
            await context.RespondAsync("Player not found", HttpStatusCode.NotFound);
            return true;
        }

        var reasonSupplied = context.RequestHeaders.TryGetValue("Reason", out var reason);
        if (!reasonSupplied)
        {
            reason = "No reason supplied";
        }

        reason += " (kicked by admin)";

        if (session == null)
        {
            await context.RespondAsync("Player not found", HttpStatusCode.NotFound);
            return true;
        }
        _netManager.DisconnectChannel(session.Channel, reason.ToString());
        await context.RespondAsync("Success", HttpStatusCode.OK);
        _sawmill.Info("Kicked player {0} ({1})", username, reason);
        return true;
    }

    /// <summary>
    ///     Round restart/end
    /// </summary>
    private async Task<bool> ActionRoundStatus(IStatusHandlerContext context)
    {
        // TODO: Any of those actions break the game ticker, fix that.
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/round")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        // Not using body, because that's a stream and I don't want to deal with that
        var actionSupplied = context.RequestHeaders.TryGetValue("Action", out var action);
        if (!actionSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }
        switch (action)
        {
            case "start":
                if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
                {
                    await context.RespondAsync("Round already started", HttpStatusCode.BadRequest);
                    _sawmill.Info("Forced round start failed: round already started");
                    return true;
                }
                _gameTicker.StartRound();
                _sawmill.Info("Forced round start");
                break;
            case "end":
                if (_gameTicker.RunLevel != GameRunLevel.InRound)
                {
                    await context.RespondAsync("Round already ended", HttpStatusCode.BadRequest);
                    _sawmill.Info("Forced round end failed: round is not in progress");
                    return true;
                }
                _gameTicker.EndRound();
                _sawmill.Info("Forced round end");
                break;
            case "restart":
                if (_gameTicker.RunLevel != GameRunLevel.InRound)
                {
                    await context.RespondAsync("Round already ended", HttpStatusCode.BadRequest);
                    _sawmill.Info("Forced round restart failed: round is not in progress");
                    return true;
                }
                _roundEndSystem.EndRound();
                _sawmill.Info("Forced round restart");
                break;
            case "restartnow": // You should restart yourself NOW!!!
                _gameTicker.RestartRound();
                _sawmill.Info("Forced instant round restart");
                break;
            default:
                await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                return true;
        }

        await context.RespondAsync("Success", HttpStatusCode.OK);
        return true;
    }
#endregion

#region Fetching

    /// <summary>
    ///     Returns an array containing all presets.
    /// </summary>
    private async Task<bool> GetForcePresets(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url!.AbsolutePath != "/admin/force_presets")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var jObject = new JsonObject();
        var presets = new List<string>();
        foreach (var preset in _prototypeManager.EnumeratePrototypes<GamePresetPrototype>())
        {
            presets.Add(preset.ID);
        }

        jObject["presets"] = JsonNode.Parse(JsonSerializer.Serialize(presets));
        await context.RespondAsync(jObject.ToString(), HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///    Returns an array containing all game rules.
    /// </summary>
    private async Task<bool> GetGameRules(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url!.AbsolutePath != "/admin/game_rules")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var jObject = new JsonObject();
        var gameRules = new List<string>();
        foreach (var gameRule in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (gameRule.Abstract)
                continue;

            if (gameRule.HasComponent<GameRuleComponent>(_componentFactory))
                gameRules.Add(gameRule.ID);
        }

        jObject["game_rules"] = JsonNode.Parse(JsonSerializer.Serialize(gameRules));
        await context.RespondAsync(jObject.ToString(), HttpStatusCode.OK);
        return true;
    }


    /// <summary>
    ///     Handles fetching information.
    /// </summary>
    private async Task<bool> InfoHandler(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url!.AbsolutePath != "/admin/info" || token == string.Empty)
        {
            return false;
            // 404
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        /*  Information to display
            Round number
            Connected players
            Active admins
            Active game rules
            Active game preset
            Active map
            MOTD
            Panic bunker status
         */

        var jObject = new JsonObject();

        jObject["round_id"] = _gameTicker.RoundId;

        var players = new List<string>();
        var onlineAdmins = new List<string>();
        var onlineAdminsDeadmined = new List<string>();

        foreach (var player in _playerManager.Sessions)
        {
            players.Add(player.UserId.UserId.ToString());
            if (_adminManager.IsAdmin(player))
            {
                onlineAdmins.Add(player.UserId.UserId.ToString());
            }
            else if (_adminManager.IsAdmin(player, true))
            {
                onlineAdminsDeadmined.Add(player.UserId.UserId.ToString());
            }
        }

        jObject["players"] = JsonNode.Parse(JsonSerializer.Serialize(players));
        jObject["admins"] = JsonNode.Parse(JsonSerializer.Serialize(onlineAdmins));
        jObject["deadmined"] = JsonNode.Parse(JsonSerializer.Serialize(onlineAdminsDeadmined));

        var gameRules = new List<string>();
        foreach (var addedGameRule in _gameTicker.GetActiveGameRules())
        {
            var meta = MetaData(addedGameRule);
            gameRules.Add(meta.EntityPrototype?.ID ?? meta.EntityPrototype?.Name ?? "Unknown");
        }

        jObject["game_rules"] = JsonNode.Parse(JsonSerializer.Serialize(gameRules));
        jObject["game_preset"] = _gameTicker.CurrentPreset?.ID;
        jObject["map"] = _gameMapManager.GetSelectedMap()?.MapName;
        jObject["motd"] = _motd;
        jObject["panic_bunker"] = new JsonObject();
        jObject["panic_bunker"]!["enabled"] = _adminSystem.PanicBunker.Enabled;
        jObject["panic_bunker"]!["disable_with_admins"] = _adminSystem.PanicBunker.DisableWithAdmins;
        jObject["panic_bunker"]!["enable_without_admins"] = _adminSystem.PanicBunker.EnableWithoutAdmins;
        jObject["panic_bunker"]!["count_deadminned_admins"] = _adminSystem.PanicBunker.CountDeadminnedAdmins;
        jObject["panic_bunker"]!["show_reason"] = _adminSystem.PanicBunker.ShowReason;
        jObject["panic_bunker"]!["min_account_age_hours"] = _adminSystem.PanicBunker.MinAccountAgeHours;
        jObject["panic_bunker"]!["min_overall_hours"] = _adminSystem.PanicBunker.MinOverallHours;

        await context.RespondAsync(jObject.ToString(), HttpStatusCode.OK);
        return true;
    }

#endregion

    private bool CheckAccess(IStatusHandlerContext context)
    {
        var auth = context.RequestHeaders.TryGetValue("Authorization", out var authToken);
        if (!auth)
        {
            _sawmill.Info(@"Unauthorized access attempt to admin API. No auth header");
            return false;
        } // No auth header, no access

        if (authToken == token)
            return true;

        // Invalid auth header, no access
        _sawmill.Info(@"Unauthorized access attempt to admin API. ""{0}"" vs ""{1}""", authToken, token);
        return false;
    }
}
