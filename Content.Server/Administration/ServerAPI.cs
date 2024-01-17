using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    [Dependency] private readonly IComponentFactory _componentFactory = default!; // Needed to circumvent the "IoC has no context on this thread" error until I figure out how to do it properly
    [Dependency] private readonly ITaskManager _taskManager = default!; // game explodes when calling stuff from the non-game thread
    [Dependency] private readonly EntityManager _entityManager = default!;

    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private string _token = string.Empty;
    private ISawmill _sawmill = default!;

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

        // Bandaid fix for the test fails
        // "System.Collections.Generic.KeyNotFoundException : The given key 'server.admin_api_token' was not present in the dictionary."
        // TODO: Figure out why this happens
        try
        {
            _config.OnValueChanged(CCVars.AdminApiToken, UpdateToken, true);
        }
        catch (Exception e)
        {
            _sawmill.Error("Failed to subscribe to config vars: {0}", e);
        }

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

        var body = await ReadJson<PanicBunkerActionBody>(context);
        var (success, actor) = await CheckActor(context, body);
        if (!success)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        if (body!.Action == null || body.Value == null)
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "An action and value are required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "An action and value are required to perform this action.",
                    ErrorType = ErrorTypes.ActionNotSpecified
                }
            }, HttpStatusCode.BadRequest);
            return true;
        }

        switch (body.Action) // TODO: This looks bad, there has to be a better way to do this.
        {
            case "enabled":
                if (!bool.TryParse(body.Value, out var enabled))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerEnabled, enabled);
                });
                break;
            case "disable_with_admins":
                if (!bool.TryParse(body.Value, out var disableWithAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerDisableWithAdmins, disableWithAdmins);
                });
                break;
            case "enable_without_admins":
                if (!bool.TryParse(body.Value, out var enableWithoutAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerEnableWithoutAdmins, enableWithoutAdmins);
                });
                break;
            case "count_deadminned_admins":
                if (!bool.TryParse(body.Value, out var countDeadminnedAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerCountDeadminnedAdmins, countDeadminnedAdmins);
                });
                break;
            case "show_reason":
                if (!bool.TryParse(body.Value, out var showReason))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerShowReason, showReason);
                });
                break;
            case "min_account_age_hours":
                if (!int.TryParse(body.Value, out var minAccountAgeHours))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerMinAccountAge, minAccountAgeHours * 60);
                });
                break;
            case "min_overall_hours":
                if (!int.TryParse(body.Value, out var minOverallHours))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerMinOverallHours, minOverallHours * 60);
                });
                break;
        }

        _sawmill.Info($"Panic bunker setting {body.Action} changed to {body.Value} by {actor!.Name} ({actor.Guid}).");
        await context.RespondAsync("Success", HttpStatusCode.OK);
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
        var (success, actor) = await CheckActor(context, motd);
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

        _taskManager.RunOnMainThread(() => _config.SetCVar(CCVars.MOTD, motd.Motd));
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
        var (success, actor) = await CheckActor(context, body);
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

        _taskManager.RunOnMainThread(() =>
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
        var (success, actor) = await CheckActor(context, body);
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
        _taskManager.RunOnMainThread(() => ticker.EndGameRule((EntityUid) gameRuleEntity));
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
        var (success, actor) = await CheckActor(context, body);
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
        var (success, actor) = await CheckActor(context,body);
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

        _taskManager.RunOnMainThread(() =>
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
        if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/admin/actions/round")
        {
            return false;
        }

        if (!CheckAccess(context))
            return true;
        var body = await ReadJson<RoundActionBody>(context);

        var (success, actor) = await CheckActor(context, body);
        if (!success)
            return true;

        // Get the action from the request body
        if (body == null || string.IsNullOrWhiteSpace(body.Action))
        {
            await context.RespondJsonAsync(new BaseResponse()
            {
                Message = "An action is required to perform this action.",
                Exception = new ExceptionData()
                {
                    Message = "An action is required to perform this action.",
                    ErrorType = ErrorTypes.ActionNotSpecified
                }
            }, HttpStatusCode.BadRequest);
            return true;
        }

        var (ticker, roundEndSystem) = await RunOnMainThread(() =>
        {
            var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();
            var roundEndSystem = _entitySystemManager.GetEntitySystem<RoundEndSystem>();
            return (ticker, roundEndSystem);
        });
        switch (body.Action)
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
                    }, HttpStatusCode.UnprocessableEntity);
                    _sawmill.Debug("Forced round start failed: round already started");
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
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
                    }, HttpStatusCode.UnprocessableEntity);
                    _sawmill.Debug("Forced round end failed: round is not in progress");
                    return true;
                }
                _taskManager.RunOnMainThread(() =>
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
                    }, HttpStatusCode.UnprocessableEntity);
                    _sawmill.Debug("Forced round restart failed: round is not in progress");
                    return true;
                }
                _taskManager.RunOnMainThread(() =>
                {
                    roundEndSystem.EndRound();
                });
                _sawmill.Info("Forced round restart");
                break;
            case "restartnow": // You should restart yourself NOW!!!
                _taskManager.RunOnMainThread(() =>
                {
                    ticker.RestartRound();
                });
                _sawmill.Info("Forced instant round restart");
                break;
            default:
                await context.RespondJsonAsync(new BaseResponse()
                {
                    Message = "Invalid action supplied.",
                    Exception = new ExceptionData()
                    {
                        Message = "Invalid action supplied.",
                        ErrorType = ErrorTypes.ActionNotSupported
                    }
                }, HttpStatusCode.BadRequest);
                return true;
        }

        _sawmill.Info($"Round {body.Action} by {actor!.Name} ({actor.Guid}).");
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
        } // No auth header, no access


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
        _sawmill.Info(@"Unauthorized access attempt to admin API. ""{0}""", authToken.ToString());
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

    private async Task<(bool, Actor? actor)> CheckActor(IStatusHandlerContext context, BaseBody? body = null)
    {
        // Try to read the actor from the request body
        var actor = body?.Actor ?? (await ReadJson<BaseBody>(context))?.Actor;
        if (actor != null)
        {
            // Check if the actor is valid, like if all the required fields are present
            if (string.IsNullOrWhiteSpace(actor.Guid) || string.IsNullOrWhiteSpace(actor.Name))
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

            return (true, actor);
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
        // Check if the request body is empty
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

    private record BaseBody
    {
        public Actor? Actor { get; init; }
    }

    private record Actor
    {
        public string? Guid { get; init; }
        public string? Name { get; init; }
        public bool IsAdmin { get; init; } = false;
        public bool IsDeadmined { get; init; } = false;
    }

    private record RoundActionBody : BaseBody
    {
        public string? Action { get; init; }
    }

    private record KickActionBody : BaseBody
    {
        public string? Guid { get; init; }
        public string? Reason { get; init; }
    }

    private record GameRuleActionBody : BaseBody
    {
        public string? GameRuleId { get; init; }
    }

    private record PresetActionBody : BaseBody
    {
        public string? PresetId { get; init; }
    }

    private record MotdActionBody : BaseBody
    {
        public string? Motd { get; init; }
    }

    private record PanicBunkerActionBody : BaseBody
    {
        public string? Action { get; init; }
        public string? Value { get; init; }
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
