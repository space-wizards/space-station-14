using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Client.IoC;
using Content.Client.Parallax.Managers;
using Content.IntegrationTests.Tests;
using Content.IntegrationTests.Tests.DeviceNetwork;
using Content.IntegrationTests.Tests.Interaction.Click;
using Content.IntegrationTests.Tests.Networking;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Maps;
using NUnit.Framework;
using Robust.Client;
using Robust.Server;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.UnitTesting;


[assembly: LevelOfParallelism(3)]

namespace Content.IntegrationTests;

public static class PoolManager
{
    private static readonly (string cvar, string value, bool tryAdd)[] ServerTestCvars =
    {
        (CCVars.DatabaseSynchronous.Name, "true", false),
        (CCVars.DatabaseSqliteDelay.Name, "0", false),
        (CCVars.HolidaysEnabled.Name, "false", false),
        (CCVars.GameMap.Name, "empty", true),
        (CCVars.GameMapForced.Name, "true", true),
        (CCVars.AdminLogsQueueSendDelay.Name, "0", true),
        (CCVars.NetPVS.Name, "false", true),
        (CCVars.NetInterp.Name, "false", true),
        (CCVars.NPCMaxUpdates.Name, "999999", true),
        (CCVars.GameMapForced.Name, "true", true),
        (CCVars.SysWinTickPeriod.Name, "0", true),
        (CCVars.ContactMinimumThreads.Name, "1", true),
        (CCVars.ContactMultithreadThreshold.Name, "999", true),
        (CCVars.PositionConstraintsMinimumThread.Name, "1", true),
        (CCVars.PositionConstraintsPerThread.Name, "999", true),
        (CCVars.VelocityConstraintMinimumThreads.Name, "1", true),
        (CCVars.VelocityConstraintsPerThread.Name, "999", true),
        (CCVars.ThreadParallelCount.Name, "1", true),
    };

    private static int PairId = 0;
    private static object PairLock = new object();
    private static List<Pair> Pairs = new();

    private static async Task ConfigurePrototypes(RobustIntegrationTest.IntegrationInstance instance,
        PoolSettings settings)
    {
        await instance.WaitPost(() =>
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var changes = new Dictionary<Type, HashSet<string>>();
            prototypeManager.LoadString(settings.ExtraPrototypes.Trim(), true, changes);
            prototypeManager.ReloadPrototypes(changes);
        });
    }

    private static async Task<RobustIntegrationTest.ServerIntegrationInstance> GenerateServer(PoolSettings poolSettings)
    {
        var options = new RobustIntegrationTest.ServerIntegrationOptions
        {
            ExtraPrototypes = poolSettings.ExtraPrototypes,
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

        options.BeforeStart += () =>
        {
            IoCManager.Resolve<IEntitySystemManager>()
                .LoadExtraSystemType<SimplePredictReconcileTest.PredictionTestEntitySystem>();
            IoCManager.Resolve<IComponentFactory>().RegisterClass<SimplePredictReconcileTest.PredictionTestComponent>();
            IoCManager.Register<ResettingEntitySystemTests.TestRoundRestartCleanupEvent>();
            IoCManager.Register<InteractionSystemTests.TestInteractionSystem>();
            IoCManager.Register<DeviceNetworkTestSystem>();
            IoCManager.Resolve<IEntitySystemManager>()
                .LoadExtraSystemType<ResettingEntitySystemTests.TestRoundRestartCleanupEvent>();
            IoCManager.Resolve<IEntitySystemManager>()
                .LoadExtraSystemType<InteractionSystemTests.TestInteractionSystem>();
            IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<DeviceNetworkTestSystem>();
            IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
        };

        SetupCVars(poolSettings, options);

        var server = new RobustIntegrationTest.ServerIntegrationInstance(options);
        await server.WaitIdleAsync();
        return server;
    }

    public static void Shutdown()
    {
        lock (PairLock)
        {
            var pairs = Pairs;
            // We are trying to make things blow up if they are still happening after this method.
            Pairs = null;
            foreach (var pair in pairs)
            {
                pair.Client.Dispose();
                pair.Server.Dispose();
            }
        }
    }

    private static async Task<RobustIntegrationTest.ClientIntegrationInstance> GenerateClient(PoolSettings poolSettings)
    {
        var options = new RobustIntegrationTest.ClientIntegrationOptions
        {
            FailureLogLevel = LogLevel.Warning,
            ContentStart = true,
            ExtraPrototypes = poolSettings.ExtraPrototypes,
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

        options.BeforeStart += () =>
        {
            IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
            {
                ClientBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>()
                        .LoadExtraSystemType<SimplePredictReconcileTest.PredictionTestEntitySystem>();
                    IoCManager.Resolve<IComponentFactory>()
                        .RegisterClass<SimplePredictReconcileTest.PredictionTestComponent>();
                    IoCManager.Register<IParallaxManager, DummyParallaxManager>(true);
                    IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
                }
            });
        };

        SetupCVars(poolSettings, options);

        var client = new RobustIntegrationTest.ClientIntegrationInstance(options);
        await client.WaitIdleAsync();
        return client;
    }

    private static void SetupCVars(PoolSettings poolSettings, RobustIntegrationTest.IntegrationOptions options)
    {
        foreach (var serverTestCvar in ServerTestCvars)
        {
            options.CVarOverrides[serverTestCvar.cvar] = serverTestCvar.value;
        }

        if (poolSettings.DummyTicker)
        {
            options.CVarOverrides[CCVars.GameDummyTicker.Name] = "true";
        }

        if (poolSettings.InLobby)
        {
            options.CVarOverrides[CCVars.GameLobbyEnabled.Name] = "true";
        }

        if (poolSettings.DisableInterpolate)
        {
            options.CVarOverrides[CCVars.NetInterp.Name] = "false";
        }

        if (poolSettings.Map != null)
        {
            options.CVarOverrides[CCVars.GameMap.Name] = poolSettings.Map;
        }
    }

    public static async Task<PairTracker> GetServerClient(PoolSettings poolSettings = null,
        [System.Runtime.CompilerServices.CallerFilePath] string testMethodFilePath = "",
        [System.Runtime.CompilerServices.CallerMemberName] string testMethodName = "") =>
        await GetServerClientPair(poolSettings ?? new PoolSettings(), $"{testMethodFilePath}, {testMethodName}");

    private static async Task<PairTracker> GetServerClientPair(PoolSettings poolSettings, string testMethodName)
    {
        Pair pair = null;
        try
        {
            var poolRetrieveTimeWatch = new Stopwatch();
            poolRetrieveTimeWatch.Start();
            await TestContext.Out.WriteLineAsync("Getting server/client");
            if (poolSettings.MustBeNew)
            {
                await TestContext.Out.WriteLineAsync($"Creating, because must be new pair");
                pair = await CreateServerClientPair(poolSettings);
            }
            else
            {
                pair = GrabOptimalPair(poolSettings);
                if (pair != null)
                {
                    var canSkip = pair.Settings.CanFastRecycle(poolSettings);

                    if (!canSkip)
                    {
                        await TestContext.Out.WriteLineAsync($"Cleaning existing pair");
                        await CleanPooledPair(poolSettings, pair);
                    }
                    else
                    {
                        await TestContext.Out.WriteLineAsync($"Skip cleanup pair");
                    }
                }
                else
                {
                    await TestContext.Out.WriteLineAsync($"Creating, because pool empty");
                    pair = await CreateServerClientPair(poolSettings);
                }
            }

            var poolRetrieveTime = poolRetrieveTimeWatch.Elapsed;
            await TestContext.Out.WriteLineAsync(
                $"Got server/client (id:{pair.PairId},uses:{pair.TestHistory.Count}) in {poolRetrieveTime.TotalMilliseconds} ms");
            pair.Settings = poolSettings;
            pair.TestHistory.Add(testMethodName);
            var usageWatch = new Stopwatch();
            usageWatch.Start();
            return new PairTracker()
            {
                Pair = pair,
                UsageWatch = usageWatch
            };
        }
        finally
        {
            if (pair != null)
            {
                TestContext.Out.WriteLine($"Test History|\n{string.Join('\n', pair.TestHistory)}\n|Test History End");
            }
        }
    }

    private static Pair GrabOptimalPair(PoolSettings poolSettings)
    {
        lock (PairLock)
        {
            if (Pairs.Count == 0) return null;
            for (var i = 0; i < Pairs.Count; i++)
            {
                var pair = Pairs[i];
                if (!pair.Settings.CanFastRecycle(poolSettings)) continue;
                Pairs.RemoveAt(i);
                return pair;
            }
            var defaultPair = Pairs[^1];
            Pairs.RemoveAt(Pairs.Count - 1);
            return defaultPair;
        }
    }

    /// <summary>
    /// Used after checking pairs, Don't use this directly
    /// </summary>
    /// <param name="pair"></param>
    public static void NoCheckReturn(Pair pair)
    {
        lock (PairLock)
        {
            Pairs.Add(pair);
        }
    }

    private static async Task CleanPooledPair(PoolSettings poolSettings, Pair pair)
    {
        var methodWatch = new Stopwatch();
        methodWatch.Start();
        await TestContext.Out.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Setting CVar ");
        var configManager = pair.Server.ResolveDependency<IConfigurationManager>();
        await pair.Server.WaitPost(() =>
        {
            configManager.SetCVar(CCVars.GameLobbyEnabled, poolSettings.InLobby);
        });
        var cNetMgr = pair.Client.ResolveDependency<IClientNetManager>();
        if (!cNetMgr.IsConnected)
        {
            await TestContext.Out.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Connecting client, and restarting server");
            pair.Client.SetConnectTarget(pair.Server);
            await pair.Server.WaitPost(() =>
            {
                EntitySystem.Get<GameTicker>().RestartRound();
            });
            await pair.Client.WaitPost(() =>
            {
                cNetMgr.ClientConnect(null!, 0, null!);
            });
        }
        await ReallyBeIdle(pair,11);

        await TestContext.Out.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Disconnecting client, and restarting server");

        await pair.Client.WaitPost(() =>
        {
            cNetMgr.ClientDisconnect("Test pooling cleanup disconnect");
        });

        await ReallyBeIdle(pair, 10);

        if (!string.IsNullOrWhiteSpace(pair.Settings.ExtraPrototypes))
        {
            await TestContext.Out.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Removing prototypes");
            if (!pair.Settings.NoServer)
            {
                var serverProtoManager = pair.Server.ResolveDependency<IPrototypeManager>();
                await pair.Server.WaitPost(() =>
                {
                    serverProtoManager.RemoveString(pair.Settings.ExtraPrototypes.Trim());
                });
            }
            if(!pair.Settings.NoClient)
            {
                var clientProtoManager = pair.Client.ResolveDependency<IPrototypeManager>();
                await pair.Client.WaitPost(() =>
                {
                    clientProtoManager.RemoveString(pair.Settings.ExtraPrototypes.Trim());
                });
            }

            await ReallyBeIdle(pair, 1);
        }

        if (poolSettings.ExtraPrototypes != null)
        {
            await TestContext.Out.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Adding prototypes");
            if (!poolSettings.NoServer)
            {
                await ConfigurePrototypes(pair.Server, poolSettings);
            }
            if (!poolSettings.NoClient)
            {
                await ConfigurePrototypes(pair.Client, poolSettings);
            }
        }

        await TestContext.Out.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Restarting server again");
        await pair.Server.WaitPost(() =>
        {
            EntitySystem.Get<GameTicker>().RestartRound();
        });


        if (!poolSettings.NotConnected)
        {
            await TestContext.Out.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Connecting client");
            await ReallyBeIdle(pair);
            pair.Client.SetConnectTarget(pair.Server);
            await pair.Client.WaitPost(() =>
            {
                var netMgr = IoCManager.Resolve<IClientNetManager>();
                if (!netMgr.IsConnected)
                {
                    netMgr.ClientConnect(null!, 0, null!);
                }
            });
        }
        await ReallyBeIdle(pair);
        await TestContext.Out.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Done recycling");
    }

    private static async Task<Pair> CreateServerClientPair(PoolSettings poolSettings)
    {
        var client = await GenerateClient(poolSettings);
        var server = await GenerateServer(poolSettings);

        var pair = new Pair { Server = server, Client = client, PairId = Interlocked.Increment(ref PairId)};
        if (!poolSettings.NotConnected)
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
            await client.WaitRunTicks(1);
        }
        return pair;
    }

    public static async Task<TestMapData> CreateTestMap(PairTracker pairTracker)
    {
        var server = pairTracker.Pair.Server;
        var settings = pairTracker.Pair.Settings;
        if (settings.NoServer) throw new Exception("Cannot setup test map without server");
        var mapData = new TestMapData();
        await server.WaitPost(() =>
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            mapData.MapId = mapManager.CreateMap();
            mapData.MapGrid = mapManager.CreateGrid(mapData.MapId);
            mapData.GridCoords = new EntityCoordinates(mapData.MapGrid.GridEntityId, 0, 0);
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var plating = tileDefinitionManager["plating"];
            var platingTile = new Tile(plating.TileId);
            mapData.MapGrid.SetTile(mapData.GridCoords, platingTile);
            mapData.MapCoords = new MapCoordinates(0, 0, mapData.MapId);
            mapData.Tile = mapData.MapGrid.GetAllTiles().First();
        });
        if (!settings.Disconnected)
        {
            await RunTicksSync(pairTracker.Pair, 10);
        }

        return mapData;
    }

    public static async Task RunTicksSync(Pair pair, int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            await pair.Server.WaitRunTicks(1);
            await pair.Client.WaitRunTicks(1);
        }
    }

    public static async Task WaitUntil(RobustIntegrationTest.IntegrationInstance instance, Func<bool> func,
        int maxTicks = 600,
        int tickStep = 1)
    {
        await WaitUntil(instance, async () => await Task.FromResult(func()), maxTicks, tickStep);
    }

    public static async Task ReallyBeIdle(Pair pair, int runTicks = 25)
    {
        for (int i = 0; i < runTicks; i++)
        {
            await pair.Client.WaitRunTicks(1);
            await pair.Server.WaitRunTicks(1);
            for (int idleCycles = 0; idleCycles < 4; idleCycles++)
            {
                await pair.Client.WaitIdleAsync();
                await pair.Server.WaitIdleAsync();
            }
        }
    }

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
}

public sealed class PoolSettings
{
    // Todo: We can make more of these pool-able, if we need enough of them for it to matter
    public bool MustNotBeReused => Destructive || NoLoadContent || DisableInterpolate || DummyTicker;
    public bool MustBeNew => Fresh || NoLoadContent || DisableInterpolate || DummyTicker;

    public bool NotConnected => NoClient || NoServer || Disconnected;
    /// <summary>
    /// We are going to ruin this pair
    /// </summary>
    public bool Destructive { get; init; }

    /// <summary>
    /// We need a brand new pair
    /// </summary>
    public bool Fresh { get; init; }

    /// <summary>
    /// We need a pair that uses a dummy ticker
    /// </summary>
    public bool DummyTicker { get; init; }

    /// <summary>
    /// We need the client, and server to be disconnected
    /// </summary>
    public bool Disconnected { get; init; }

    /// <summary>
    /// We need the server to be in the lobby
    /// </summary>
    public bool InLobby { get; init; }

    /// <summary>
    /// We don't want content loaded
    /// </summary>
    public bool NoLoadContent { get; init; }

    /// <summary>
    /// We want to add some prototypes
    /// </summary>
    public string ExtraPrototypes { get; init; }

    /// <summary>
    /// Disables NetInterp
    /// </summary>
    public bool DisableInterpolate { get; init; }

    /// <summary>
    /// Tells the pool it has to clean up before the server/client can be used.
    /// </summary>
    public bool Dirty { get; init; }

    /// <summary>
    /// Sets the map Cvar, and loads the map
    /// </summary>
    public string Map { get; init; } // TODO for map painter

    /// <summary>
    /// The test won't use the client (so we can skip cleaning it)
    /// </summary>
    public bool NoClient { get; init; }

    /// <summary>
    /// The test won't use the client (so we can skip cleaning it)
    /// </summary>
    public bool NoServer { get; init; }

    /// <summary>
    /// Guess if skipping recycling is ok
    /// </summary>
    /// <param name="nextSettings">The next set of settings the old pair will be set to</param>
    /// <returns></returns>
    public bool CanFastRecycle(PoolSettings nextSettings)
    {
        if (Dirty) return false;
        if (Destructive || nextSettings.Destructive) return false;
        if (NotConnected != nextSettings.NotConnected) return false;
        if (InLobby != nextSettings.InLobby) return false;
        if (DisableInterpolate != nextSettings.DisableInterpolate) return false;
        if (nextSettings.DummyTicker) return false;
        if (Map != nextSettings.Map) return false;
        if (NoLoadContent != nextSettings.NoLoadContent) return false;
        if (nextSettings.Fresh) return false;
        if (ExtraPrototypes != nextSettings.ExtraPrototypes) return false;
        return true;
    }
}

public sealed class TestMapData
{
    public MapId MapId { get; set; }
    public IMapGrid MapGrid { get; set; }
    public EntityCoordinates GridCoords { get; set; }
    public MapCoordinates MapCoords { get; set; }
    public TileRef Tile { get; set; }
}

public sealed class Pair
{
    public int PairId { get; init; }
    public List<string> TestHistory { get; set; } = new();
    public PoolSettings Settings { get; set; }
    public RobustIntegrationTest.ServerIntegrationInstance Server { get; init; }
    public RobustIntegrationTest.ClientIntegrationInstance Client { get; init; }
}

public sealed class PairTracker : IAsyncDisposable
{
    private int _disposed;

    public async Task OnDirtyDispose()
    {
        var usageTime = UsageWatch.Elapsed;
        await TestContext.Out.WriteLineAsync($"Dirty: Test returned in {usageTime.TotalMilliseconds} ms");
        var dirtyWatch = new Stopwatch();
        dirtyWatch.Start();
        Pair.Client.Dispose();
        Pair.Server.Dispose();
        var disposeTime = dirtyWatch.Elapsed;
        await TestContext.Out.WriteLineAsync($"Dirty: Disposed in {disposeTime.TotalMilliseconds} ms");
    }

    public async Task OnCleanDispose()
    {
        var usageTime = UsageWatch.Elapsed;
        await TestContext.Out.WriteLineAsync($"Clean: Test returned in {usageTime.TotalMilliseconds} ms");
        var cleanWatch = new Stopwatch();
        cleanWatch.Start();
        // Let any last minute failures the test cause happen.
        await PoolManager.ReallyBeIdle(Pair);
        if (!Pair.Settings.Destructive)
        {
            if (Pair.Client.IsAlive == false)
            {
                throw new Exception("Test killed the client", Pair.Client.UnhandledException);
            }

            if (Pair.Server.IsAlive == false)
            {
                throw new Exception("Test killed the server", Pair.Server.UnhandledException);
            }
        }

        if (Pair.Settings.MustNotBeReused)
        {
            Pair.Client.Dispose();
            Pair.Server.Dispose();
            var returnTime2 = cleanWatch.Elapsed;
            await TestContext.Out.WriteLineAsync($"Clean: Clean disposed in {returnTime2.TotalMilliseconds} ms");
            return;
        }

        var sRuntimeLog = Pair.Server.ResolveDependency<IRuntimeLog>();
        if (sRuntimeLog.ExceptionCount > 0) throw new Exception("Server logged exceptions");
        var cRuntimeLog = Pair.Client.ResolveDependency<IRuntimeLog>();
        if (cRuntimeLog.ExceptionCount > 0) throw new Exception("Client logged exceptions");
        PoolManager.NoCheckReturn(Pair);
        var returnTime = cleanWatch.Elapsed;
        await TestContext.Out.WriteLineAsync($"Clean: Clean returned to pool in {returnTime.TotalMilliseconds} ms");
    }
    public Stopwatch UsageWatch { get; set; }
    public Pair Pair { get; init; }

    public async ValueTask CleanReturnAsync()
    {
        var disposed = Interlocked.Exchange(ref _disposed, 1);
        switch (disposed)
        {
            case 0:
                await TestContext.Out.WriteLineAsync("Clean Return Start");
                break;
            case 1:
                throw new Exception("Already called clean return before");
            case 2:
                throw new Exception("Already dirty disposed");
            default:
                throw new Exception("Unexpected disposed value");
        }

        await OnCleanDispose();
        await TestContext.Out.WriteLineAsync($"Clean Return Exiting");
    }

    public async ValueTask DisposeAsync()
    {
        var disposed = Interlocked.Exchange(ref _disposed, 2);
        switch (disposed)
        {
            case 0:
                await TestContext.Out.WriteLineAsync("Dirty Return Start");
                break;
            case 1:
                await TestContext.Out.WriteLineAsync("Dirty Return - Already Clean Disposed");
                return;
            case 2:
                throw new Exception("Already called dirty return before");
            default:
                throw new Exception("Unexpected disposed value");
        }

        await OnDirtyDispose();
        await TestContext.Out.WriteLineAsync($"Dirty Return Exiting");
    }
}
