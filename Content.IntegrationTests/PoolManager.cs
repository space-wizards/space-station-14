#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Content.Client.IoC;
using Content.Client.Parallax.Managers;
using Content.IntegrationTests.Tests;
using Content.IntegrationTests.Tests.Destructible;
using Content.IntegrationTests.Tests.DeviceNetwork;
using Content.IntegrationTests.Tests.Interaction.Click;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.UnitTesting;

[assembly: LevelOfParallelism(3)]

namespace Content.IntegrationTests;

/// <summary>
/// Making clients, and servers is slow, this manages a pool of them so tests can reuse them.
/// </summary>
public static partial class PoolManager
{
    public const string TestMap = "Empty";

    private static readonly (string cvar, string value)[] TestCvars =
    {
        // @formatter:off
        (CCVars.DatabaseSynchronous.Name,     "true"),
        (CCVars.DatabaseSqliteDelay.Name,     "0"),
        (CCVars.HolidaysEnabled.Name,         "false"),
        (CCVars.GameMap.Name,                 TestMap),
        (CCVars.AdminLogsQueueSendDelay.Name, "0"),
        (CVars.NetPVS.Name,                   "false"),
        (CCVars.NPCMaxUpdates.Name,           "999999"),
        (CVars.ThreadParallelCount.Name,      "1"),
        (CCVars.GameRoleTimers.Name,          "false"),
        (CCVars.GridFill.Name,                "false"),
        (CCVars.ArrivalsShuttles.Name,        "false"),
        (CCVars.EmergencyShuttleEnabled.Name, "false"),
        (CCVars.ProcgenPreload.Name,          "false"),
        (CCVars.WorldgenEnabled.Name,         "false"),
        (CVars.ReplayClientRecordingEnabled.Name, "false"),
        (CVars.ReplayServerRecordingEnabled.Name, "false"),
        (CCVars.GameDummyTicker.Name, "true"),
        (CCVars.GameLobbyEnabled.Name, "false"),
        (CCVars.ConfigPresetDevelopment.Name, "false"),
        (CCVars.AdminLogsEnabled.Name, "false"),

        // This breaks some tests.
        // TODO: Figure out which tests this breaks.
        (CVars.NetBufferSize.Name, "0")

        // @formatter:on
    };

    private static int _pairId;
    private static readonly object PairLock = new();
    private static bool _initialized;

    // Pair, IsBorrowed
    private static readonly Dictionary<Pair, bool> Pairs = new();
    private static bool _dead;
    private static Exception? _poolFailureReason;

    private static async Task<(RobustIntegrationTest.ServerIntegrationInstance, PoolTestLogHandler)> GenerateServer(
        PoolSettings poolSettings,
        TextWriter testOut)
    {
        var options = new RobustIntegrationTest.ServerIntegrationOptions
        {
            ContentStart = true,
            Options = new ServerOptions()
            {
                LoadConfigAndUserData = false,
                LoadContentResources = !poolSettings.NoLoadContent,
            },
            ContentAssemblies = new[]
            {
                typeof(Shared.Entry.EntryPoint).Assembly,
                typeof(Server.Entry.EntryPoint).Assembly,
                typeof(PoolManager).Assembly
            }
        };

        var logHandler = new PoolTestLogHandler("SERVER");
        logHandler.ActivateContext(testOut);
        options.OverrideLogHandler = () => logHandler;

        options.BeforeStart += () =>
        {
            var entSysMan = IoCManager.Resolve<IEntitySystemManager>();
            var compFactory = IoCManager.Resolve<IComponentFactory>();
            entSysMan.LoadExtraSystemType<ResettingEntitySystemTests.TestRoundRestartCleanupEvent>();
            entSysMan.LoadExtraSystemType<InteractionSystemTests.TestInteractionSystem>();
            entSysMan.LoadExtraSystemType<DeviceNetworkTestSystem>();
            entSysMan.LoadExtraSystemType<TestDestructibleListenerSystem>();
            IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
            IoCManager.Resolve<IConfigurationManager>()
                .OnValueChanged(RTCVars.FailureLogLevel, value => logHandler.FailureLevel = value, true);
        };

        SetDefaultCVars(options);
        var server = new RobustIntegrationTest.ServerIntegrationInstance(options);
        await server.WaitIdleAsync();
        await SetupCVars(server, poolSettings);
        return (server, logHandler);
    }

    /// <summary>
    /// This shuts down the pool, and disposes all the server/client pairs.
    /// This is a one time operation to be used when the testing program is exiting.
    /// </summary>
    public static void Shutdown()
    {
        List<Pair> localPairs;
        lock (PairLock)
        {
            if (_dead)
                return;
            _dead = true;
            localPairs = Pairs.Keys.ToList();
        }

        foreach (var pair in localPairs)
        {
            pair.Kill();
        }

        _initialized = false;
    }

    public static string DeathReport()
    {
        lock (PairLock)
        {
            var builder = new StringBuilder();
            var pairs = Pairs.Keys.OrderBy(pair => pair.PairId);
            foreach (var pair in pairs)
            {
                var borrowed = Pairs[pair];
                builder.AppendLine($"Pair {pair.PairId}, Tests Run: {pair.TestHistory.Count}, Borrowed: {borrowed}");
                for (var i = 0; i < pair.TestHistory.Count; i++)
                {
                    builder.AppendLine($"#{i}: {pair.TestHistory[i]}");
                }
            }

            return builder.ToString();
        }
    }

    private static async Task<(RobustIntegrationTest.ClientIntegrationInstance, PoolTestLogHandler)> GenerateClient(
        PoolSettings poolSettings,
        TextWriter testOut)
    {
        var options = new RobustIntegrationTest.ClientIntegrationOptions
        {
            FailureLogLevel = LogLevel.Warning,
            ContentStart = true,
            ContentAssemblies = new[]
            {
                typeof(Shared.Entry.EntryPoint).Assembly,
                typeof(Client.Entry.EntryPoint).Assembly,
                typeof(PoolManager).Assembly
            }
        };

        if (poolSettings.NoLoadContent)
        {
            Assert.Warn("NoLoadContent does not work on the client, ignoring");
        }

        options.Options = new GameControllerOptions()
        {
            LoadConfigAndUserData = false,
            // LoadContentResources = !poolSettings.NoLoadContent
        };

        var logHandler = new PoolTestLogHandler("CLIENT");
        logHandler.ActivateContext(testOut);
        options.OverrideLogHandler = () => logHandler;

        options.BeforeStart += () =>
        {
            IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
            {
                ClientBeforeIoC = () =>
                {
                    // do not register extra systems or components here -- they will get cleared when the client is
                    // disconnected. just use reflection.
                    IoCManager.Register<IParallaxManager, DummyParallaxManager>(true);
                    IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
                    IoCManager.Resolve<IConfigurationManager>()
                        .OnValueChanged(RTCVars.FailureLogLevel, value => logHandler.FailureLevel = value, true);
                }
            });
        };

        SetDefaultCVars(options);
        var client = new RobustIntegrationTest.ClientIntegrationInstance(options);
        await client.WaitIdleAsync();
        await SetupCVars(client, poolSettings);
        return (client, logHandler);
    }

    private static async Task  SetupCVars(RobustIntegrationTest.IntegrationInstance instance, PoolSettings settings)
    {
        var cfg = instance.ResolveDependency<IConfigurationManager>();
        await instance.WaitPost(() =>
        {
            if (cfg.IsCVarRegistered(CCVars.GameDummyTicker.Name))
                cfg.SetCVar(CCVars.GameDummyTicker, settings.UseDummyTicker);

            if (cfg.IsCVarRegistered(CCVars.GameLobbyEnabled.Name))
                cfg.SetCVar(CCVars.GameLobbyEnabled, settings.InLobby);

            if (cfg.IsCVarRegistered(CVars.NetInterp.Name))
                cfg.SetCVar(CVars.NetInterp, settings.DisableInterpolate);

            if (cfg.IsCVarRegistered(CCVars.GameMap.Name))
                cfg.SetCVar(CCVars.GameMap, settings.Map);

            if (cfg.IsCVarRegistered(CCVars.AdminLogsEnabled.Name))
                cfg.SetCVar(CCVars.AdminLogsEnabled, settings.AdminLogsEnabled);

            if (cfg.IsCVarRegistered(CVars.NetInterp.Name))
                cfg.SetCVar(CVars.NetInterp, !settings.DisableInterpolate);
        });
    }

    private static void SetDefaultCVars(RobustIntegrationTest.IntegrationOptions options)
    {
        foreach (var (cvar, value) in TestCvars)
        {
            options.CVarOverrides[cvar] = value;
        }
    }

    /// <summary>
    /// Gets a <see cref="PairTracker"/>, which can be used to get access to a server, and client <see cref="Pair"/>
    /// </summary>
    /// <param name="poolSettings">See <see cref="PoolSettings"/></param>
    /// <returns></returns>
    public static async Task<PairTracker> GetServerClient(PoolSettings? poolSettings = null)
    {
        return await GetServerClientPair(poolSettings ?? new PoolSettings());
    }

    private static string GetDefaultTestName(TestContext testContext)
    {
        return testContext.Test.FullName.Replace("Content.IntegrationTests.Tests.", "");
    }

    private static async Task<PairTracker> GetServerClientPair(PoolSettings poolSettings)
    {
        if (!_initialized)
            throw new InvalidOperationException($"Pool manager has not been initialized");

        // Trust issues with the AsyncLocal that backs this.
        var testContext = TestContext.CurrentContext;
        var testOut = TestContext.Out;

        DieIfPoolFailure();
        var currentTestName = poolSettings.TestName ?? GetDefaultTestName(testContext);
        var poolRetrieveTimeWatch = new Stopwatch();
        await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Called by test {currentTestName}");
        Pair? pair = null;
        try
        {
            poolRetrieveTimeWatch.Start();
            if (poolSettings.MustBeNew)
            {
                await testOut.WriteLineAsync(
                    $"{nameof(GetServerClientPair)}: Creating pair, because settings of pool settings");
                pair = await CreateServerClientPair(poolSettings, testOut);

                // Newly created pairs should always be in a valid state.
                await RunTicksSync(pair, 5);
                await SyncTicks(pair, targetDelta: 1);
                ValidatePair(pair, poolSettings);
            }
            else
            {
                await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Looking in pool for a suitable pair");
                pair = GrabOptimalPair(poolSettings);
                if (pair != null)
                {
                    pair.ActivateContext(testOut);

                    await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Suitable pair found");
                    var canSkip = pair.Settings.CanFastRecycle(poolSettings);

                    if (canSkip)
                    {
                        await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Cleanup not needed, Skipping cleanup of pair");
                        await SetupCVars(pair.Client, poolSettings);
                        await SetupCVars(pair.Server, poolSettings);
                        await RunTicksSync(pair, 1);
                    }
                    else
                    {
                        await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Cleaning existing pair");
                        await CleanPooledPair(poolSettings, pair, testOut);
                    }

                    await RunTicksSync(pair, 5);
                    await SyncTicks(pair, targetDelta: 1);
                    ValidatePair(pair, poolSettings);
                }
                else
                {
                    await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Creating a new pair, no suitable pair found in pool");
                    pair = await CreateServerClientPair(poolSettings, testOut);
                }
            }

        }
        finally
        {
            if (pair != null && pair.TestHistory.Count > 1)
            {
                await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Pair {pair.PairId} Test History Start");
                for (var i = 0; i < pair.TestHistory.Count; i++)
                {
                    await testOut.WriteLineAsync($"- Pair {pair.PairId} Test #{i}: {pair.TestHistory[i]}");
                }
                await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Pair {pair.PairId} Test History End");
            }
        }
        var poolRetrieveTime = poolRetrieveTimeWatch.Elapsed;
        await testOut.WriteLineAsync(
            $"{nameof(GetServerClientPair)}: Retrieving pair {pair.PairId} from pool took {poolRetrieveTime.TotalMilliseconds} ms");
        await testOut.WriteLineAsync(
            $"{nameof(GetServerClientPair)}: Returning pair {pair.PairId}");
        pair.Settings = poolSettings;
        pair.TestHistory.Add(currentTestName);
        var usageWatch = new Stopwatch();
        usageWatch.Start();

        return new PairTracker(testOut)
        {
            Pair = pair,
            UsageWatch = usageWatch
        };
    }

    private static void ValidatePair(Pair pair, PoolSettings settings)
    {
        var cfg = pair.Server.ResolveDependency<IConfigurationManager>();
        Assert.That(cfg.GetCVar(CCVars.AdminLogsEnabled), Is.EqualTo(settings.AdminLogsEnabled));
        Assert.That(cfg.GetCVar(CCVars.GameLobbyEnabled), Is.EqualTo(settings.InLobby));
        Assert.That(cfg.GetCVar(CCVars.GameDummyTicker), Is.EqualTo(settings.UseDummyTicker));

        var entMan = pair.Server.ResolveDependency<EntityManager>();
        var ticker = entMan.System<GameTicker>();
        Assert.That(ticker.DummyTicker, Is.EqualTo(settings.UseDummyTicker));

        var expectPreRound = settings.InLobby | settings.DummyTicker;
        var expectedLevel = expectPreRound ? GameRunLevel.PreRoundLobby : GameRunLevel.InRound;
        Assert.That(ticker.RunLevel, Is.EqualTo(expectedLevel));

        var baseClient = pair.Client.ResolveDependency<IBaseClient>();
        var netMan = pair.Client.ResolveDependency<INetManager>();
        Assert.That(netMan.IsConnected, Is.Not.EqualTo(!settings.ShouldBeConnected));

        if (!settings.ShouldBeConnected)
            return;

        Assert.That(baseClient.RunLevel, Is.EqualTo(ClientRunLevel.InGame));
        var cPlayer = pair.Client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
        var sPlayer = pair.Server.ResolveDependency<IPlayerManager>();
        Assert.That(sPlayer.Sessions.Count(), Is.EqualTo(1));
        var session = sPlayer.Sessions.Single();
        Assert.That(cPlayer.LocalPlayer?.Session.UserId, Is.EqualTo(session.UserId));

        if (ticker.DummyTicker)
            return;

        var status = ticker.PlayerGameStatuses[session.UserId];
        var expected = settings.InLobby
            ? PlayerGameStatus.NotReadyToPlay
            : PlayerGameStatus.JoinedGame;

        Assert.That(status, Is.EqualTo(expected));

        if (settings.InLobby)
        {
            Assert.Null(session.AttachedEntity);
            return;
        }

        Assert.NotNull(session.AttachedEntity);
        Assert.That(entMan.EntityExists(session.AttachedEntity));
        Assert.That(entMan.HasComponent<MindContainerComponent>(session.AttachedEntity));
        var mindCont = entMan.GetComponent<MindContainerComponent>(session.AttachedEntity!.Value);
        Assert.NotNull(mindCont.Mind);
        Assert.Null(mindCont.Mind?.VisitingEntity);
        Assert.That(mindCont.Mind!.OwnedEntity, Is.EqualTo(session.AttachedEntity!.Value));
        Assert.That(mindCont.Mind.UserId, Is.EqualTo(session.UserId));
    }

    private static Pair? GrabOptimalPair(PoolSettings poolSettings)
    {
        lock (PairLock)
        {
            Pair? fallback = null;
            foreach (var pair in Pairs.Keys)
            {
                if (Pairs[pair])
                    continue;
                if (!pair.Settings.CanFastRecycle(poolSettings))
                {
                    fallback = pair;
                    continue;
                }
                Pairs[pair] = true;
                return pair;
            }

            if (fallback != null)
            {
                Pairs[fallback!] = true;
            }
            return fallback;
        }
    }

    /// <summary>
    /// Used by PairTracker after checking the server/client pair, Don't use this.
    /// </summary>
    /// <param name="pair"></param>
    public static void NoCheckReturn(Pair pair)
    {
        lock (PairLock)
        {
            if (pair.Dead)
            {
                Pairs.Remove(pair);
            }
            else
            {
                Pairs[pair] = false;
            }
        }
    }

    private static async Task CleanPooledPair(PoolSettings settings, Pair pair, TextWriter testOut)
    {
        pair.Settings = default!;
        var methodWatch = new Stopwatch();
        methodWatch.Start();
        await testOut.WriteLineAsync($"Recycling...");

        var configManager = pair.Server.ResolveDependency<IConfigurationManager>();
        var entityManager = pair.Server.ResolveDependency<IEntityManager>();
        var gameTicker = entityManager.System<GameTicker>();
        var cNetMgr = pair.Client.ResolveDependency<IClientNetManager>();

        await RunTicksSync(pair, 1);

        // Disconnect the client if they are connected.
        if (cNetMgr.IsConnected)
        {
            await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Disconnecting client.");
            await pair.Client.WaitPost(() => cNetMgr.ClientDisconnect("Test pooling cleanup disconnect"));
            await RunTicksSync(pair, 1);
        }
        Assert.That(cNetMgr.IsConnected, Is.False);

        // Move to pre-round lobby. Required to toggle dummy ticker on and off
        if (gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Restarting server.");
            Assert.That(gameTicker.DummyTicker, Is.False);
            configManager.SetCVar(CCVars.GameLobbyEnabled, true);
            await pair.Server.WaitPost(() => gameTicker.RestartRound());
            await RunTicksSync(pair, 1);
        }

        //Apply Cvars
        await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Setting CVar ");
        await SetupCVars(pair.Client, settings);
        await SetupCVars(pair.Server, settings);
        await RunTicksSync(pair, 1);

        // Restart server.
        await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Restarting server again");
        await pair.Server.WaitPost(() => gameTicker.RestartRound());
        await RunTicksSync(pair, 1);

        // Connect client
        if (settings.ShouldBeConnected)
        {
            await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Connecting client");
            pair.Client.SetConnectTarget(pair.Server);
            await pair.Client.WaitPost(() => cNetMgr.ClientConnect(null!, 0, null!));
        }

        await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Idling");
        await ReallyBeIdle(pair);
        await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Done recycling");
    }

    private static void DieIfPoolFailure()
    {
        if (_poolFailureReason != null)
        {
            // If the _poolFailureReason is not null, we can assume at least one test failed.
            // So we say inconclusive so we don't add more failed tests to search through.
            Assert.Inconclusive(@"
In a different test, the pool manager had an exception when trying to create a server/client pair.
Instead of risking that the pool manager will fail at creating a server/client pairs for every single test,
we are just going to end this here to save a lot of time. This is the exception that started this:\n {0}", _poolFailureReason);
        }

        if (_dead)
        {
            // If Pairs is null, we ran out of time, we can't assume a test failed.
            // So we are going to tell it all future tests are a failure.
            Assert.Fail("The pool was shut down");
        }
    }

    private static async Task<Pair> CreateServerClientPair(PoolSettings poolSettings, TextWriter testOut)
    {
        Pair pair;
        try
        {
            var (client, clientLog) = await GenerateClient(poolSettings, testOut);
            var (server, serverLog) = await GenerateServer(poolSettings, testOut);
            pair = new Pair
            {
                Server = server,
                ServerLogHandler = serverLog,
                Client = client,
                ClientLogHandler = clientLog,
                PairId = Interlocked.Increment(ref _pairId)
            };

            if (!poolSettings.NoLoadTestPrototypes)
                await pair.LoadPrototypes(_testPrototypes!);
        }
        catch (Exception ex)
        {
            _poolFailureReason = ex;
            throw;
        }

        if (!poolSettings.UseDummyTicker)
        {
            var gameTicker = pair.Server.ResolveDependency<IEntityManager>().System<GameTicker>();
            await pair.Server.WaitPost(() => gameTicker.RestartRound());
        }

        if (poolSettings.ShouldBeConnected)
        {
            pair.Client.SetConnectTarget(pair.Server);
            await pair.Client.WaitPost(() =>
            {
                var netMgr = IoCManager.Resolve<IClientNetManager>();
                if (!netMgr.IsConnected)
                {
                    netMgr.ClientConnect(null!, 0, null!);
                }
            });
            await ReallyBeIdle(pair, 10);
            await pair.Client.WaitRunTicks(1);
        }
        return pair;
    }

    /// <summary>
    /// Creates a map, a grid, and a tile, and gives back references to them.
    /// </summary>
    /// <param name="pairTracker">A pairTracker</param>
    /// <returns>A TestMapData</returns>
    public static async Task<TestMapData> CreateTestMap(PairTracker pairTracker)
    {
        var server = pairTracker.Pair.Server;

        await server.WaitIdleAsync();

        var settings = pairTracker.Pair.Settings;
        var mapManager = server.ResolveDependency<IMapManager>();
        var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

        var mapData = new TestMapData();
        await server.WaitPost(() =>
        {
            mapData.MapId = mapManager.CreateMap();
            mapData.MapUid = mapManager.GetMapEntityId(mapData.MapId);
            mapData.MapGrid = mapManager.CreateGrid(mapData.MapId);
            mapData.GridUid = mapData.MapGrid.Owner; // Fixing this requires an engine PR.
            mapData.GridCoords = new EntityCoordinates(mapData.GridUid, 0, 0);
            var plating = tileDefinitionManager["Plating"];
            var platingTile = new Tile(plating.TileId);
            mapData.MapGrid.SetTile(mapData.GridCoords, platingTile);
            mapData.MapCoords = new MapCoordinates(0, 0, mapData.MapId);
            mapData.Tile = mapData.MapGrid.GetAllTiles().First();
        });
        if (settings.ShouldBeConnected)
        {
            await RunTicksSync(pairTracker.Pair, 10);
        }

        return mapData;
    }

    /// <summary>
    /// Runs a server/client pair in sync
    /// </summary>
    /// <param name="pair">A server/client pair</param>
    /// <param name="ticks">How many ticks to run them for</param>
    public static async Task RunTicksSync(Pair pair, int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            await pair.Server.WaitRunTicks(1);
            await pair.Client.WaitRunTicks(1);
        }
    }

    /// <summary>
    /// Runs the server/client in sync, but also ensures they are both idle each tick.
    /// </summary>
    /// <param name="pair">The server/client pair</param>
    /// <param name="runTicks">How many ticks to run</param>
    public static async Task ReallyBeIdle(Pair pair, int runTicks = 25)
    {
        for (var i = 0; i < runTicks; i++)
        {
            await pair.Client.WaitRunTicks(1);
            await pair.Server.WaitRunTicks(1);
            for (var idleCycles = 0; idleCycles < 4; idleCycles++)
            {
                await pair.Client.WaitIdleAsync();
                await pair.Server.WaitIdleAsync();
            }
        }
    }

    /// <summary>
    /// Run the server/clients until the ticks are synchronized.
    /// By default the client will be one tick ahead of the server.
    /// </summary>
    public static async Task SyncTicks(Pair pair, int targetDelta = 1)
    {
        var sTiming = pair.Server.ResolveDependency<IGameTiming>();
        var cTiming = pair.Client.ResolveDependency<IGameTiming>();
        var sTick = (int)sTiming.CurTick.Value;
        var cTick = (int)cTiming.CurTick.Value;
        var delta = cTick - sTick;

        if (delta == targetDelta)
            return;
        if (delta > targetDelta)
            await pair.Server.WaitRunTicks(delta - targetDelta);
        else
            await pair.Client.WaitRunTicks(targetDelta - delta);

        sTick = (int)sTiming.CurTick.Value;
        cTick = (int)cTiming.CurTick.Value;
        delta = cTick - sTick;
        Assert.That(delta, Is.EqualTo(targetDelta));
    }

    /// <summary>
    /// Runs a server, or a client until a condition is true
    /// </summary>
    /// <param name="instance">The server or client</param>
    /// <param name="func">The condition to check</param>
    /// <param name="maxTicks">How many ticks to try before giving up</param>
    /// <param name="tickStep">How many ticks to wait between checks</param>
    public static async Task WaitUntil(RobustIntegrationTest.IntegrationInstance instance, Func<bool> func,
        int maxTicks = 600,
        int tickStep = 1)
    {
        await WaitUntil(instance, async () => await Task.FromResult(func()), maxTicks, tickStep);
    }

    /// <summary>
    /// Runs a server, or a client until a condition is true
    /// </summary>
    /// <param name="instance">The server or client</param>
    /// <param name="func">The async condition to check</param>
    /// <param name="maxTicks">How many ticks to try before giving up</param>
    /// <param name="tickStep">How many ticks to wait between checks</param>
    public static async Task WaitUntil(RobustIntegrationTest.IntegrationInstance instance, Func<Task<bool>> func,
        int maxTicks = 600,
        int tickStep = 1)
    {
        var ticksAwaited = 0;
        bool passed;

        await instance.WaitIdleAsync();

        while (!(passed = await func()) && ticksAwaited < maxTicks)
        {
            var ticksToRun = tickStep;

            if (ticksAwaited + tickStep > maxTicks)
            {
                ticksToRun = maxTicks - ticksAwaited;
            }

            await instance.WaitRunTicks(ticksToRun);

            ticksAwaited += ticksToRun;
        }

        if (!passed)
        {
            Assert.Fail($"Condition did not pass after {maxTicks} ticks.\n" +
                        $"Tests ran ({instance.TestsRan.Count}):\n" +
                        $"{string.Join('\n', instance.TestsRan)}");
        }

        Assert.That(passed);
    }

    /// <summary>
    ///     Helper method that retrieves all entity prototypes that have some component.
    /// </summary>
    public static List<EntityPrototype> GetEntityPrototypes<T>(RobustIntegrationTest.IntegrationInstance instance) where T : Component
    {
        var protoMan = instance.ResolveDependency<IPrototypeManager>();
        var compFact = instance.ResolveDependency<IComponentFactory>();

        var id = compFact.GetComponentName(typeof(T));
        var list = new List<EntityPrototype>();
        foreach (var ent in protoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (ent.Components.ContainsKey(id))
                list.Add(ent);
        }

        return list;
    }

    /// <summary>
    /// Initialize the pool manager.
    /// </summary>
    /// <param name="assembly">Assembly to search for to discover extra test prototypes.</param>
    public static void Startup(Assembly? assembly)
    {
        if (_initialized)
            throw new InvalidOperationException("Already initialized");

        _initialized = true;
        DiscoverTestPrototypes(assembly);
    }
}

/// <summary>
/// Settings for the pooled server, and client pair.
/// Some options are for changing the pair, and others are
/// so the pool can properly clean up what you borrowed.
/// </summary>
public sealed class PoolSettings
{
    /// <summary>
    /// If the returned pair must not be reused
    /// </summary>
    public bool MustNotBeReused => Destructive || NoLoadContent || NoLoadTestPrototypes;

    /// <summary>
    /// If the given pair must be brand new
    /// </summary>
    public bool MustBeNew => Fresh || NoLoadContent || NoLoadTestPrototypes;

    /// <summary>
    /// Set to true if the test will ruin the server/client pair.
    /// </summary>
    public bool Destructive { get; init; }

    /// <summary>
    /// Set to true if the given server/client pair should be created fresh.
    /// </summary>
    public bool Fresh { get; init; }

    /// <summary>
    /// Set to true if the given server should be using a dummy ticker. Ignored if <see cref="InLobby"/> is true.
    /// </summary>
    public bool DummyTicker { get; init; } = true;

    public bool UseDummyTicker => !InLobby && DummyTicker;

    /// <summary>
    /// If true, this enables the creation of admin logs during the test.
    /// </summary>
    public bool AdminLogsEnabled { get; init; }

    /// <summary>
    /// Set to true if the given server/client pair should be connected from each other.
    /// Defaults to disconnected as it makes dirty recycling slightly faster.
    /// If <see cref="InLobby"/> is true, this option is ignored.
    /// </summary>
    public bool Connected { get; init; }

    public bool ShouldBeConnected => InLobby || Connected;

    /// <summary>
    /// Set to true if the given server/client pair should be in the lobby.
    /// If the pair is not in the lobby at the end of the test, this test must be marked as dirty.
    /// </summary>
    /// <remarks>
    /// If this is enabled, the value of <see cref="DummyTicker"/> is ignored.
    /// </remarks>
    public bool InLobby { get; init; }

    /// <summary>
    /// Set this to true to skip loading the content files.
    /// Note: This setting won't work with a client.
    /// </summary>
    public bool NoLoadContent { get; init; }

    /// <summary>
    /// This will return a server-client pair that has not loaded test prototypes.
    /// Try avoiding this whenever possible, as this will always  create & destroy a new pair.
    /// Use <see cref="Pair.IsTestPrototype(EntityPrototype)"/> if you need to exclude test prototypees.
    /// </summary>
    public bool NoLoadTestPrototypes { get; init; }

    /// <summary>
    /// Set this to true to disable the NetInterp CVar on the given server/client pair
    /// </summary>
    public bool DisableInterpolate { get; init; }

    /// <summary>
    /// Set this to true to always clean up the server/client pair before giving it to another borrower
    /// </summary>
    public bool Dirty { get; init; }

    /// <summary>
    /// Set this to the path of a map to have the given server/client pair load the map.
    /// </summary>
    public string Map { get; init; } = PoolManager.TestMap;

    /// <summary>
    /// Overrides the test name detection, and uses this in the test history instead
    /// </summary>
    public string? TestName { get; set; }

    /// <summary>
    /// Tries to guess if we can skip recycling the server/client pair.
    /// </summary>
    /// <param name="nextSettings">The next set of settings the old pair will be set to</param>
    /// <returns>If we can skip cleaning it up</returns>
    public bool CanFastRecycle(PoolSettings nextSettings)
    {
        if (MustNotBeReused)
            throw new InvalidOperationException("Attempting to recycle a non-reusable test.");

        if (nextSettings.MustBeNew)
            throw new InvalidOperationException("Attempting to recycle a test while requesting a fresh test.");

        if (Dirty)
            return false;

        // Check that certain settings match.
        return !ShouldBeConnected == !nextSettings.ShouldBeConnected
               && UseDummyTicker == nextSettings.UseDummyTicker
               && Map == nextSettings.Map
               && InLobby == nextSettings.InLobby;
    }
}

/// <summary>
/// Holds a reference to things commonly needed when testing on a map
/// </summary>
public sealed class TestMapData
{
    public EntityUid MapUid { get; set; }
    public EntityUid GridUid { get; set; }
    public MapId MapId { get; set; }
    public MapGridComponent MapGrid { get; set; } = default!;
    public EntityCoordinates GridCoords { get; set; }
    public MapCoordinates MapCoords { get; set; }
    public TileRef Tile { get; set; }
}

/// <summary>
/// A server/client pair
/// </summary>
public sealed class Pair
{
    public bool Dead { get; private set; }
    public int PairId { get; init; }
    public List<string> TestHistory { get; set; } = new();
    public PoolSettings Settings { get; set; } = default!;
    public RobustIntegrationTest.ServerIntegrationInstance Server { get; init; } = default!;
    public RobustIntegrationTest.ClientIntegrationInstance Client { get; init; } = default!;

    public PoolTestLogHandler ServerLogHandler { get; init; } = default!;
    public PoolTestLogHandler ClientLogHandler { get; init; } = default!;

    private Dictionary<Type, HashSet<string>> _loadedPrototypes = new();
    private HashSet<string> _loadedEntityPrototypes = new();

    public void Kill()
    {
        Dead = true;
        Server.Dispose();
        Client.Dispose();
    }

    public void ClearContext()
    {
        ServerLogHandler.ClearContext();
        ClientLogHandler.ClearContext();
    }

    public void ActivateContext(TextWriter testOut)
    {
        ServerLogHandler.ActivateContext(testOut);
        ClientLogHandler.ActivateContext(testOut);
    }

    public async Task LoadPrototypes(List<string> prototypes)
    {
        await LoadPrototypes(Server, prototypes);
        await LoadPrototypes(Client, prototypes);
    }

    private async Task LoadPrototypes(RobustIntegrationTest.IntegrationInstance instance, List<string> prototypes)
    {
        var changed = new Dictionary<Type, HashSet<string>>();
        var protoMan = instance.ResolveDependency<IPrototypeManager>();
        foreach (var file in prototypes)
        {
            protoMan.LoadString(file, changed: changed);
        }

        await instance.WaitPost(() => protoMan.ReloadPrototypes(changed));

        foreach (var (kind, ids) in changed)
        {
            _loadedPrototypes.GetOrNew(kind).UnionWith(ids);
        }

        if (_loadedPrototypes.TryGetValue(typeof(EntityPrototype), out var entIds))
            _loadedEntityPrototypes.UnionWith(entIds);
    }

    public bool IsTestPrototype(EntityPrototype proto)
    {
        return _loadedEntityPrototypes.Contains(proto.ID);
    }

    public bool IsTestEntityPrototype(string id)
    {
        return _loadedEntityPrototypes.Contains(id);
    }

    public bool IsTestPrototype<TPrototype>(string id) where TPrototype : IPrototype
    {
        return IsTestPrototype(typeof(TPrototype), id);
    }

    public bool IsTestPrototype<TPrototype>(TPrototype proto) where TPrototype : IPrototype
    {
        return IsTestPrototype(typeof(TPrototype), proto.ID);
    }

    public bool IsTestPrototype(Type kind, string id)
    {
        return _loadedPrototypes.TryGetValue(kind, out var ids) && ids.Contains(id);
    }
}

/// <summary>
/// Used by the pool to keep track of a borrowed server/client pair.
/// </summary>
public sealed class PairTracker : IAsyncDisposable
{
    private readonly TextWriter _testOut;
    private int _disposed;
    public Stopwatch UsageWatch { get; set; } = default!;
    public Pair Pair { get; init; } = default!;

    public PairTracker(TextWriter testOut)
    {
        _testOut = testOut;
    }

    // Convenience properties.
    public RobustIntegrationTest.ServerIntegrationInstance Server => Pair.Server;
    public RobustIntegrationTest.ClientIntegrationInstance Client => Pair.Client;

    private async Task OnDirtyDispose()
    {
        var usageTime = UsageWatch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Test gave back pair {Pair.PairId} in {usageTime.TotalMilliseconds} ms");
        var dirtyWatch = new Stopwatch();
        dirtyWatch.Start();
        Pair.Kill();
        PoolManager.NoCheckReturn(Pair);
        var disposeTime = dirtyWatch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Disposed pair {Pair.PairId} in {disposeTime.TotalMilliseconds} ms");

        // Test pairs should only dirty dispose if they are failing. If they are not failing, this probably happened
        // because someone forgot to clean-return the pair.
        Assert.Warn("Test was dirty-disposed.");
    }

    private async Task OnCleanDispose()
    {
        var usageTime = UsageWatch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Test borrowed pair {Pair.PairId} for {usageTime.TotalMilliseconds} ms");
        var cleanWatch = new Stopwatch();
        cleanWatch.Start();
        // Let any last minute failures the test cause happen.
        await PoolManager.ReallyBeIdle(Pair);
        if (!Pair.Settings.Destructive)
        {
            if (Pair.Client.IsAlive == false)
            {
                throw new Exception($"{nameof(CleanReturnAsync)}: Test killed the client in pair {Pair.PairId}:", Pair.Client.UnhandledException);
            }

            if (Pair.Server.IsAlive == false)
            {
                throw new Exception($"{nameof(CleanReturnAsync)}: Test killed the server in pair {Pair.PairId}:", Pair.Server.UnhandledException);
            }
        }

        if (Pair.Settings.MustNotBeReused)
        {
            Pair.Kill();
            PoolManager.NoCheckReturn(Pair);
            await PoolManager.ReallyBeIdle(Pair);
            var returnTime2 = cleanWatch.Elapsed;
            await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Clean disposed in {returnTime2.TotalMilliseconds} ms");
            return;
        }

        var sRuntimeLog = Pair.Server.ResolveDependency<IRuntimeLog>();
        if (sRuntimeLog.ExceptionCount > 0)
            throw new Exception($"{nameof(CleanReturnAsync)}: Server logged exceptions");
        var cRuntimeLog = Pair.Client.ResolveDependency<IRuntimeLog>();
        if (cRuntimeLog.ExceptionCount > 0)
            throw new Exception($"{nameof(CleanReturnAsync)}: Client logged exceptions");

        Pair.ClearContext();
        PoolManager.NoCheckReturn(Pair);
        var returnTime = cleanWatch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: PoolManager took {returnTime.TotalMilliseconds} ms to put pair {Pair.PairId} back into the pool");
    }

    public async ValueTask CleanReturnAsync()
    {
        var disposed = Interlocked.Exchange(ref _disposed, 1);
        switch (disposed)
        {
            case 0:
                await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Return of pair {Pair.PairId} started");
                break;
            case 1:
                throw new Exception($"{nameof(CleanReturnAsync)}: Already clean returned");
            case 2:
                throw new Exception($"{nameof(CleanReturnAsync)}: Already dirty disposed");
            default:
                throw new Exception($"{nameof(CleanReturnAsync)}: Unexpected disposed value");
        }

        await OnCleanDispose();
    }

    public async ValueTask DisposeAsync()
    {
        var disposed = Interlocked.Exchange(ref _disposed, 2);
        switch (disposed)
        {
            case 0:
                await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Dirty return of pair {Pair.PairId} started");
                break;
            case 1:
                await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Pair {Pair.PairId} was properly clean disposed");
                return;
            case 2:
                throw new Exception($"{nameof(DisposeAsync)}: Already dirty disposed pair {Pair.PairId}");
            default:
                throw new Exception($"{nameof(DisposeAsync)}: Unexpected disposed value");
        }
        await OnDirtyDispose();
    }
}
