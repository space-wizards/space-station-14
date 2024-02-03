using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Maps;
using Content.Server.RoundEnd;
using Content.Shared.Administration.Events;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Prototypes;
using Robust.Server.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration;

public sealed class ServerApi : IPostInjectInit
{
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!; // Players
    [Dependency] private readonly ISharedAdminManager _adminManager = default!; // Admins
    [Dependency] private readonly IGameMapManager _gameMapManager = default!; // Map name
    [Dependency] private readonly IServerNetManager _netManager = default!; // Kick
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Game rules
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!; // game explodes when calling stuff from the non-game thread
    [Dependency] private readonly EntityManager _entityManager = default!;

    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private string _token = string.Empty;
    private ISawmill _sawmill = default!;

    public static Dictionary<string, string> PanicPunkerCvarNames = new()
    {
        { "Enabled", "game.panic_bunker.enabled" },
        { "DisableWithAdmins", "game.panic_bunker.disable_with_admins" },
        { "EnableWithoutAdmins", "game.panic_bunker.enable_without_admins" },
        { "CountDeadminnedAdmins", "game.panic_bunker.count_deadminned_admins" },
        { "ShowReason", "game.panic_bunker.show_reason" },
        { "MinAccountAgeHours", "game.panic_bunker.min_account_age" },
        { "MinOverallHours", "game.panic_bunker.min_overall_hours" },
        { "CustomReason", "game.panic_bunker.custom_reason" }
    };

    void IPostInjectInit.PostInject()
    {
        _sawmill = Logger.GetSawmill("serverApi");

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
    }

    public void Initialize()
    {
        _config.OnValueChanged(CCVars.AdminApiToken, UpdateToken, true);
    }

    public void Shutdown()
    {
        _config.UnsubValueChanged(CCVars.AdminApiToken, UpdateToken);
    }

    private void UpdateToken(string token)
    {
        _token = token;
    }


#region Actions

    /// <summary>
    ///     Changes the panic bunker settings.
    /// </summary>
    private async Task<bool> ActionPanicPunker(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Patch || context.Url.AbsolutePath != "/admin/actions/panic_bunker")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var body = await ReadJson<Dictionary<string, object>>(context);
        var (success, actor) = await CheckActor(context);
        if (!success)
            return true;

        foreach (var panicPunkerActions in body!.Select(x => new { Action = x.Key, Value = x.Value.ToString() }))
        {
            if (panicPunkerActions.Action == null || panicPunkerActions.Value == null)
            {
                await context.RespondJsonAsync(new BaseResponse()
                {
                    Message = "Action and value are required to perform this action.",
                    Exception = new ExceptionData()
                    {
                        Message = "Action and value are required to perform this action.",
                        ErrorType = ErrorTypes.ActionNotSpecified
                    }
                }, HttpStatusCode.BadRequest);
                return true;
            }

            if (!PanicPunkerCvarNames.TryGetValue(panicPunkerActions.Action, out var cvarName))
            {
                await context.RespondJsonAsync(new BaseResponse()
                {
                    Message = $"Cannot set: Action {panicPunkerActions.Action} does not exist.",
                    Exception = new ExceptionData()
                    {
                        Message = $"Cannot set: Action {panicPunkerActions.Action} does not exist.",
                        ErrorType = ErrorTypes.ActionNotSupported
                    }
                }, HttpStatusCode.BadRequest);
                return true;
            }

            // Since the CVar can be of different types, we need to parse it to the correct type
            // First, I try to parse it as a bool, if it fails, I try to parse it as an int
            // And as a last resort, I do nothing and put it as a string
            if (bool.TryParse(panicPunkerActions.Value, out var boolValue))
            {
                await RunOnMainThread(() => _config.SetCVar(cvarName, boolValue));
            }
            else if (int.TryParse(panicPunkerActions.Value, out var intValue))
            {
                await RunOnMainThread(() => _config.SetCVar(cvarName, intValue));
            }
            else
            {
                await RunOnMainThread(() => _config.SetCVar(cvarName, panicPunkerActions.Value));
            }
            _sawmill.Info($"Panic bunker property {panicPunkerActions} changed to {panicPunkerActions.Value} by {actor!.Name} ({actor.Guid}).");
        }

        await context.RespondJsonAsync(new BaseResponse()
        {
            Message = "OK"
        });
        return true;
    }

    /// <summary>
    ///     Sets the current MOTD.
    /// </summary>
    private async Task<bool> ActionForceMotd(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/admin/actions/set_motd")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var motd = await ReadJson<MotdActionBody>(context);
        var (success, actor) = await CheckActor(context);
        if (!success)
            return true;


        if (motd!.Motd == null)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "A motd is required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "A motd is required to perform this action.",
                    ErrorType = ErrorTypes.MotdNotSpecified
                }
            }, HttpStatusCode.BadRequest);
            return true;
        }

        _sawmill.Info($"MOTD changed to \"{motd.Motd}\" by {actor!.Name} ({actor.Guid}).");

        await RunOnMainThread(() => _config.SetCVar(CCVars.MOTD, motd.Motd));
        // A hook in the MOTD system sends the changes to each client
        await context.RespondJsonAsync(new BaseResponse()
        {
            Message = "OK"
        });
        return true;
    }

    /// <summary>
    ///     Forces the next preset-
    /// </summary>
    private async Task<bool> ActionForcePreset(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/admin/actions/force_preset")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var body = await ReadJson<PresetActionBody>(context);
        var (success, actor) = await CheckActor(context);
        if (!success)
            return true;

        var ticker = await RunOnMainThread(() => _entitySystemManager.GetEntitySystem<GameTicker>());

        if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "Round already started",
                Exception = new ExceptionData()
                {
                    Message = "Round already started",
                    ErrorType = ErrorTypes.RoundAlreadyStarted
                }
            }, HttpStatusCode.Conflict);
            return true;
        }

        if (body!.PresetId == null)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "A preset is required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "A preset is required to perform this action.",
                    ErrorType = ErrorTypes.PresetNotSpecified
                }
            }, HttpStatusCode.BadRequest);
            return true;
        }

        var result = await RunOnMainThread(() => ticker.FindGamePreset(body.PresetId));
        if (result == null)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "Preset not found",
                Exception = new ExceptionData()
                {
                    Message = "Preset not found",
                    ErrorType = ErrorTypes.PresetNotSpecified
                }
            }, HttpStatusCode.UnprocessableContent);
            return true;
        }

        await RunOnMainThread(() =>
        {
            ticker.SetGamePreset(result);
        });
        _sawmill.Info($"Forced the game to start with preset {body.PresetId} by {actor!.Name}({actor.Guid}).");
        await context.RespondJsonAsync(new BaseResponse()
        {
            Message = "OK"
        });
        return true;
    }

    /// <summary>
    ///     Ends an active game rule.
    /// </summary>
    private async Task<bool> ActionEndGameRule(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/admin/actions/end_game_rule")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var body = await ReadJson<GameRuleActionBody>(context);
        var (success, actor) = await CheckActor(context);
        if (!success)
            return true;

        if (body!.GameRuleId == null)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "A game rule is required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "A game rule is required to perform this action.",
                    ErrorType = ErrorTypes.GuidNotSpecified
                }
            }, HttpStatusCode.BadRequest);
            return true;
        }
        var ticker = await RunOnMainThread(() => _entitySystemManager.GetEntitySystem<GameTicker>());

        var gameRuleEntity = await RunOnMainThread(() => ticker
            .GetActiveGameRules()
            .FirstOrNull(rule => _entityManager.MetaQuery.GetComponent(rule).EntityPrototype?.ID == body.GameRuleId));

        if (gameRuleEntity == null) // Game rule not found
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "Game rule not found or not active",
                Exception = new ExceptionData()
                {
                    Message = "Game rule not found or not active",
                    ErrorType = ErrorTypes.GameRuleNotFound
                }
            }, HttpStatusCode.Conflict);
            return true;
        }

        _sawmill.Info($"Ended game rule {body.GameRuleId} by {actor!.Name} ({actor.Guid}).");
        await RunOnMainThread(() => ticker.EndGameRule((EntityUid) gameRuleEntity));
        await context.RespondJsonAsync(new BaseResponse()
        {
            Message = "OK"
        });
        return true;
    }

    /// <summary>
    ///     Adds a game rule to the current round.
    /// </summary>
    private async Task<bool> ActionAddGameRule(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/admin/actions/add_game_rule")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var body = await ReadJson<GameRuleActionBody>(context);
        var (success, actor) = await CheckActor(context);
        if (!success)
            return true;

        if (body!.GameRuleId == null)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "A game rule is required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "A game rule is required to perform this action.",
                    ErrorType = ErrorTypes.GuidNotSpecified
                }
            }, HttpStatusCode.BadRequest);
            return true;
        }

        var ruleEntity = await RunOnMainThread<EntityUid?>(() =>
        {
            var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();
            // See if prototype exists
            try
            {
                _prototypeManager.Index(body.GameRuleId);
            }
            catch (KeyNotFoundException e)
            {
                return null;
            }

            var ruleEntity = ticker.AddGameRule(body.GameRuleId);
            _sawmill.Info($"Added game rule {body.GameRuleId} by {actor!.Name} ({actor.Guid}).");
            if (ticker.RunLevel == GameRunLevel.InRound)
            {
                ticker.StartGameRule(ruleEntity);
                _sawmill.Info($"Started game rule {body.GameRuleId} by {actor.Name} ({actor.Guid}).");
            }
            return ruleEntity;
        });
        if (ruleEntity == null)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "Game rule not found",
                Exception = new ExceptionData()
                {
                    Message = "Game rule not found",
                    ErrorType = ErrorTypes.GameRuleNotFound
                }
            }, HttpStatusCode.UnprocessableContent);
            return true;
        }

        await context.RespondJsonAsync(new BaseResponse()
        {
            Message = "OK"
        });
        return true;
    }

    /// <summary>
    ///     Kicks a player.
    /// </summary>
    private async Task<bool> ActionKick(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/admin/actions/kick")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var body = await ReadJson<KickActionBody>(context);
        var (success, actor) = await CheckActor(context);
        if (!success)
            return true;

        if (body == null)
        {
            _sawmill.Info($"Attempted to kick player without supplying a body by {actor!.Name}({actor.Guid}).");
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "A body is required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "A body is required to perform this action.",
                    ErrorType = ErrorTypes.BodyUnableToParse
                }
            }, HttpStatusCode.BadRequest);
            return true;
        }

        if (body.Guid == null)
        {
            _sawmill.Info($"Attempted to kick player without supplying a username by {actor!.Name}({actor.Guid}).");
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "A player is required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "A player is required to perform this action.",
                    ErrorType = ErrorTypes.GuidNotSpecified
                }
            }, HttpStatusCode.BadRequest);
            return true;
        }

        var session = await RunOnMainThread(() =>
        {
            _playerManager.TryGetSessionById(new NetUserId(new Guid(body.Guid)), out var player);
            return player;
        });

        if (session == null)
        {
            _sawmill.Info($"Attempted to kick player {body.Guid} by {actor!.Name} ({actor.Guid}), but they were not found.");
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "Player not found",
                Exception = new ExceptionData()
                {
                    Message = "Player not found",
                    ErrorType = ErrorTypes.PlayerNotFound
                }
            }, HttpStatusCode.UnprocessableContent);
            return true;
        }

        var reason = body.Reason ?? "No reason supplied";
        reason += " (kicked by admin)";

        await RunOnMainThread(() =>
        {
            _netManager.DisconnectChannel(session.Channel, reason);
        });
        await context.RespondJsonAsync(new BaseResponse()
        {
            Message = "OK"
        });
        _sawmill.Info("Kicked player {0} ({1}) for {2} by {3}({4})", session.Name, session.UserId.UserId.ToString(), reason, actor!.Name, actor.Guid);
        return true;
    }

    /// <summary>
    ///     Round restart/end
    /// </summary>
    private async Task<bool> ActionRoundStatus(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || !context.Url.AbsolutePath.StartsWith("/admin/actions/round/"))
        {
            return false;
        }

        // Make sure paths like /admin/actions/round/lol/start don't work
        if (context.Url.AbsolutePath.Split('/').Length != 5)
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;
        var (success, actor) = await CheckActor(context);
        if (!success)
            return true;


        var (ticker, roundEndSystem) = await RunOnMainThread(() =>
        {
            var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();
            var roundEndSystem = _entitySystemManager.GetEntitySystem<RoundEndSystem>();
            return (ticker, roundEndSystem);
        });

        // Action is the last part of the URL path (e.g. /admin/actions/round/start -> start)
        var action = context.Url.AbsolutePath.Split('/').Last();

        switch (action)
        {
            case "start":
                if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
                {
                    await context.RespondJsonAsync(new BaseResponse()
                    {
                        Message = "Round already started",
                        Exception = new ExceptionData()
                        {
                            Message = "Round already started",
                            ErrorType = ErrorTypes.RoundAlreadyStarted
                        }
                    }, HttpStatusCode.Conflict);
                    _sawmill.Debug("Forced round start failed: round already started");
                    return true;
                }

                await RunOnMainThread(() =>
                {
                    ticker.StartRound();
                });
                _sawmill.Info("Forced round start");
                break;
            case "end":
                if (ticker.RunLevel != GameRunLevel.InRound)
                {
                    await context.RespondJsonAsync(new BaseResponse()
                    {
                        Message = "Round already ended",
                        Exception = new ExceptionData()
                        {
                            Message = "Round already ended",
                            ErrorType = ErrorTypes.RoundAlreadyEnded
                        }
                    }, HttpStatusCode.Conflict);
                    _sawmill.Debug("Forced round end failed: round is not in progress");
                    return true;
                }
                await RunOnMainThread(() =>
                {
                    roundEndSystem.EndRound();
                });
                _sawmill.Info("Forced round end");
                break;
            case "restart":
                if (ticker.RunLevel != GameRunLevel.InRound)
                {
                    await context.RespondJsonAsync(new BaseResponse()
                    {
                        Message = "Round not in progress",
                        Exception = new ExceptionData()
                        {
                            Message = "Round not in progress",
                            ErrorType = ErrorTypes.RoundNotInProgress
                        }
                    }, HttpStatusCode.Conflict);
                    _sawmill.Debug("Forced round restart failed: round is not in progress");
                    return true;
                }
                await RunOnMainThread(() =>
                {
                    roundEndSystem.EndRound();
                });
                _sawmill.Info("Forced round restart");
                break;
            case "restartnow": // You should restart yourself NOW!!!
                await RunOnMainThread(() =>
                {
                    ticker.RestartRound();
                });
                _sawmill.Info("Forced instant round restart");
                break;
            default:
                return false;
        }

        _sawmill.Info($"Round {action} by {actor!.Name} ({actor.Guid}).");
        await context.RespondJsonAsync(new BaseResponse()
        {
            Message = "OK"
        });
        return true;
    }
#endregion

#region Fetching

    /// <summary>
    ///     Returns an array containing all available presets.
    /// </summary>
    private async Task<bool> GetForcePresets(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url.AbsolutePath != "/admin/force_presets")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var presets = new List<(string id, string desc)>();
        foreach (var preset in _prototypeManager.EnumeratePrototypes<GamePresetPrototype>())
        {
            presets.Add((preset.ID, preset.Description));
        }

        await context.RespondJsonAsync(new PresetResponse()
        {
            Presets = presets
        });
        return true;
    }

    /// <summary>
    ///    Returns an array containing all game rules.
    /// </summary>
    private async Task<bool> GetGameRules(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url.AbsolutePath != "/admin/game_rules")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var gameRules = new List<string>();
        foreach (var gameRule in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (gameRule.Abstract)
                continue;

            if (gameRule.HasComponent<GameRuleComponent>(_componentFactory))
                gameRules.Add(gameRule.ID);
        }

        await context.RespondJsonAsync(new GameruleResponse()
        {
            GameRules = gameRules
        });
        return true;
    }


    /// <summary>
    ///     Handles fetching information.
    /// </summary>
    private async Task<bool> InfoHandler(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url.AbsolutePath != "/admin/info")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;

        var (success, actor) = await CheckActor(context);
        if (!success)
            return true;

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

        var (ticker, adminSystem) = await RunOnMainThread(() =>
        {
            var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();
            var adminSystem = _entitySystemManager.GetEntitySystem<AdminSystem>();
            return (ticker, adminSystem);
        });

        var players = new List<Actor>();
        await RunOnMainThread(async () =>
        {
            foreach (var player in _playerManager.Sessions)
            {
                var isAdmin = _adminManager.IsAdmin(player);
                var isDeadmined = _adminManager.IsAdmin(player, true) && !isAdmin;

                players.Add(new Actor()
                {
                    Guid = player.UserId.UserId.ToString(),
                    Name = player.Name,
                    IsAdmin = isAdmin,
                    IsDeadmined = isDeadmined
                });
            }
        });
        var gameRules = await RunOnMainThread(() =>
        {
            var gameRules = new List<string>();
            foreach (var addedGameRule in ticker.GetActiveGameRules())
            {
                var meta = _entityManager.MetaQuery.GetComponent(addedGameRule);
                gameRules.Add(meta.EntityPrototype?.ID ?? meta.EntityPrototype?.Name ?? "Unknown");
            }

            return gameRules;
        });

        _sawmill.Info($"Info requested by {actor!.Name} ({actor.Guid}).");
        await context.RespondJsonAsync(new InfoResponse()
        {
            Players = players,
            RoundId = ticker.RoundId,
            Map = await RunOnMainThread(() => _gameMapManager.GetSelectedMap()?.MapName ?? "Unknown"),
            PanicBunker = adminSystem.PanicBunker,
            GamePreset = ticker.CurrentPreset?.ID,
            GameRules = gameRules,
            MOTD = _config.GetCVar(CCVars.MOTD)
        });
        return true;
    }

#endregion

    private bool CheckAccess(IStatusHandlerContext context)
    {
        var auth = context.RequestHeaders.TryGetValue("Authorization", out var authToken);
        if (!auth)
        {
            context.RespondJsonAsync(new BaseResponse()
            {
                Message = "An authorization header is required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "An authorization header is required to perform this action.",
                    ErrorType = ErrorTypes.MissingAuthentication
                }
            });
            return false;
        }


        if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(authToken.ToString()), Encoding.UTF8.GetBytes(_token)))
            return true;

        context.RespondJsonAsync(new BaseResponse()
        {
            Message = "Invalid authorization header.",
            Exception = new ExceptionData()
            {
                Message = "Invalid authorization header.",
                ErrorType = ErrorTypes.InvalidAuthentication
            }
        });
        // Invalid auth header, no access
        _sawmill.Info("Unauthorized access attempt to admin API.");
        return false;
    }

    /// <summary>
    /// Async helper function which runs a task on the main thread and returns the result.
    /// </summary>
    private async Task<T> RunOnMainThread<T>(Func<T> func)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();
        _taskManager.RunOnMainThread(() =>
        {
            try
            {
                taskCompletionSource.TrySetResult(func());
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
        });

        var result = await taskCompletionSource.Task;
        return result;
    }

    /// <summary>
    /// Runs an action on the main thread. This does not return any value and is meant to be used for void functions. Use <see cref="RunOnMainThread{T}"/> for functions that return a value.
    /// </summary>
    private async Task RunOnMainThread(Action action)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        _taskManager.RunOnMainThread(() =>
        {
            try
            {
                action();
                taskCompletionSource.TrySetResult(true);
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
        });

        await taskCompletionSource.Task;
    }

    private async Task<(bool, Actor? actor)> CheckActor(IStatusHandlerContext context)
    {
        // The actor is JSON encoded in the header
        var actor = context.RequestHeaders.TryGetValue("Actor", out var actorHeader) ? actorHeader.ToString() : null;
        if (actor != null)
        {
            var actionData = JsonSerializer.Deserialize<Actor>(actor);
            if (actionData == null)
            {
                await context.RespondJsonAsync(new BaseResponse()
                {
                    Message = "Unable to parse actor.",
                    Exception = new ExceptionData()
                    {
                        Message = "Unable to parse actor.",
                        ErrorType = ErrorTypes.BodyUnableToParse
                    }
                }, HttpStatusCode.BadRequest);
                return (false, null);
            }
            // Check if the actor is valid, like if all the required fields are present
            if (string.IsNullOrWhiteSpace(actionData.Guid) || string.IsNullOrWhiteSpace(actionData.Name))
            {
                await context.RespondJsonAsync(new BaseResponse()
                {
                    Message = "Invalid actor supplied.",
                    Exception = new ExceptionData()
                    {
                        Message = "Invalid actor supplied.",
                        ErrorType = ErrorTypes.InvalidActor
                    }
                }, HttpStatusCode.BadRequest);
                return (false, null);
            }

            // See if the parsed GUID is a valid GUID
            if (!Guid.TryParse(actionData.Guid, out _))
            {
                await context.RespondJsonAsync(new BaseResponse()
                {
                    Message = "Invalid GUID supplied.",
                    Exception = new ExceptionData()
                    {
                        Message = "Invalid GUID supplied.",
                        ErrorType = ErrorTypes.InvalidActor
                    }
                }, HttpStatusCode.BadRequest);
                return (false, null);
            }

            return (true, actionData);
        }

        await context.RespondJsonAsync(new BaseResponse()
        {
            Message = "An actor is required to perform this action.",
            Exception = new ExceptionData()
            {
                Message = "An actor is required to perform this action.",
                ErrorType = ErrorTypes.MissingActor
            }
        }, HttpStatusCode.BadRequest);
        return (false, null);
    }

    /// <summary>
    /// Helper function to read JSON encoded data from the request body.
    /// </summary>
    private async Task<T?> ReadJson<T>(IStatusHandlerContext context)
    {
        try
        {
            var json = await context.RequestBodyJsonAsync<T>();
            return json;
        }
        catch (Exception e)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "Unable to parse request body.",
                Exception = new ExceptionData()
                {
                    Message = e.Message,
                    ErrorType = ErrorTypes.BodyUnableToParse,
                    StackTrace = e.StackTrace
                }
            }, HttpStatusCode.BadRequest);
            return default;
        }
    }

#region From Client

    private record Actor
    {
        public string? Guid { get; init; }
        public string? Name { get; init; }
        public bool IsAdmin { get; init; } = false;
        public bool IsDeadmined { get; init; } = false;
    }

    private record KickActionBody
    {
        public string? Guid { get; init; }
        public string? Reason { get; init; }
    }

    private record GameRuleActionBody
    {
        public string? GameRuleId { get; init; }
    }

    private record PresetActionBody
    {
        public string? PresetId { get; init; }
    }

    private record MotdActionBody
    {
        public string? Motd { get; init; }
    }

#endregion

#region Responses

    private record BaseResponse
    {
        public string? Message { get; init; } = "OK";
        public ExceptionData? Exception { get; init; } = null;
    }

    private record ExceptionData
    {
        public string Message { get; init; } = string.Empty;
        public ErrorTypes ErrorType { get; init; } = ErrorTypes.None;
        public string? StackTrace { get; init; } = null;
    }

    private enum ErrorTypes
    {
        BodyUnableToParse = -2,
        None = -1,
        MissingAuthentication = 0,
        InvalidAuthentication = 1,
        MissingActor = 2,
        InvalidActor = 3,
        RoundNotInProgress = 4,
        RoundAlreadyStarted = 5,
        RoundAlreadyEnded = 6,
        ActionNotSpecified = 7,
        ActionNotSupported = 8,
        GuidNotSpecified = 9,
        PlayerNotFound = 10,
        GameRuleNotFound = 11,
        PresetNotSpecified = 12,
        MotdNotSpecified = 13
    }

#endregion

#region Misc

    /// <summary>
    /// Record used to send the response for the info endpoint.
    /// </summary>
    private record InfoResponse
    {
        public int RoundId { get; init; } = 0;
        public List<Actor> Players { get; init; } = new();
        public List<string> GameRules { get; init; } = new();
        public string? GamePreset { get; init; } = null;
        public string? Map { get; init; } = null;
        public string? MOTD { get; init; } = null;
        public PanicBunkerStatus PanicBunker { get; init; } = new();
    }

    private record PresetResponse : BaseResponse
    {
        public List<(string id, string desc)> Presets { get; init; } = new();
    }

    private record GameruleResponse : BaseResponse
    {
        public List<string> GameRules { get; init; } = new();
    }

#endregion

}
