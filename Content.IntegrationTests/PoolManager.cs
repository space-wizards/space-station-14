#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Content.Client.IoC;
using Content.Client.Parallax.Managers;
using Content.IntegrationTests.Pair;
using Content.IntegrationTests.Tests;
using Content.IntegrationTests.Tests.Destructible;
using Content.IntegrationTests.Tests.DeviceNetwork;
using Content.IntegrationTests.Tests.Interaction.Click;
using Robust.Client;
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.UnitTesting;

namespace Content.IntegrationTests;

/// <summary>
/// Making clients, and servers is slow, this manages a pool of them so tests can reuse them.
/// </summary>
public static partial class PoolManager
{
    public const string TestMap = "Empty";
    private static int _pairId;
    private static readonly object PairLock = new();
    private static bool _initialized;

    // Pair, IsBorrowed
    private static readonly Dictionary<TestPair, bool> Pairs = new();
    private static bool _dead;
    private static Exception? _poolFailureReason;

    private static HashSet<Assembly> _contentAssemblies = default!;

    public static async Task<(RobustIntegrationTest.ServerIntegrationInstance, PoolTestLogHandler)> GenerateServer(
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
            ContentAssemblies = _contentAssemblies.ToArray()
        };

        var logHandler = new PoolTestLogHandler("SERVER");
        logHandler.ActivateContext(testOut);
        options.OverrideLogHandler = () => logHandler;

        options.BeforeStart += () =>
        {
            // Server-only systems (i.e., systems that subscribe to events with server-only components)
            var entSysMan = IoCManager.Resolve<IEntitySystemManager>();
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
        List<TestPair> localPairs;
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
            var pairs = Pairs.Keys.OrderBy(pair => pair.Id);
            foreach (var pair in pairs)
            {
                var borrowed = Pairs[pair];
                builder.AppendLine($"Pair {pair.Id}, Tests Run: {pair.TestHistory.Count}, Borrowed: {borrowed}");
                for (var i = 0; i < pair.TestHistory.Count; i++)
                {
                    builder.AppendLine($"#{i}: {pair.TestHistory[i]}");
                }
            }

            return builder.ToString();
        }
    }

    public static async Task<(RobustIntegrationTest.ClientIntegrationInstance, PoolTestLogHandler)> GenerateClient(
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
                typeof(PoolManager).Assembly,
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

    /// <summary>
    /// Gets a <see cref="Pair.TestPair"/>, which can be used to get access to a server, and client <see cref="Pair.TestPair"/>
    /// </summary>
    /// <param name="poolSettings">See <see cref="PoolSettings"/></param>
    /// <returns></returns>
    public static async Task<TestPair> GetServerClient(PoolSettings? poolSettings = null)
    {
        return await GetServerClientPair(poolSettings ?? new PoolSettings());
    }

    private static string GetDefaultTestName(TestContext testContext)
    {
        return testContext.Test.FullName.Replace("Content.IntegrationTests.Tests.", "");
    }

    private static async Task<TestPair> GetServerClientPair(PoolSettings poolSettings)
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
        TestPair? pair = null;
        try
        {
            poolRetrieveTimeWatch.Start();
            if (poolSettings.MustBeNew)
            {
                await testOut.WriteLineAsync(
                    $"{nameof(GetServerClientPair)}: Creating pair, because settings of pool settings");
                pair = await CreateServerClientPair(poolSettings, testOut);
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
                        await pair.RunTicksSync(1);
                    }
                    else
                    {
                        await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Cleaning existing pair");
                        await pair.CleanPooledPair(poolSettings, testOut);
                    }

                    await pair.RunTicksSync(5);
                    await pair.SyncTicks(targetDelta: 1);
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
            if (pair != null && pair.TestHistory.Count > 0)
            {
                await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Pair {pair.Id} Test History Start");
                for (var i = 0; i < pair.TestHistory.Count; i++)
                {
                    await testOut.WriteLineAsync($"- Pair {pair.Id} Test #{i}: {pair.TestHistory[i]}");
                }
                await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Pair {pair.Id} Test History End");
            }
        }

        pair.ValidateSettings(poolSettings);

        var poolRetrieveTime = poolRetrieveTimeWatch.Elapsed;
        await testOut.WriteLineAsync(
            $"{nameof(GetServerClientPair)}: Retrieving pair {pair.Id} from pool took {poolRetrieveTime.TotalMilliseconds} ms");

        pair.ClearModifiedCvars();
        pair.Settings = poolSettings;
        pair.TestHistory.Add(currentTestName);
        pair.SetupSeed();
        await testOut.WriteLineAsync(
            $"{nameof(GetServerClientPair)}: Returning pair {pair.Id} with client/server seeds: {pair.ClientSeed}/{pair.ServerSeed}");

        pair.Watch.Restart();
        return pair;
    }

    private static TestPair? GrabOptimalPair(PoolSettings poolSettings)
    {
        lock (PairLock)
        {
            TestPair? fallback = null;
            foreach (var pair in Pairs.Keys)
            {
                if (Pairs[pair])
                    continue;

                if (!pair.Settings.CanFastRecycle(poolSettings))
                {
                    fallback = pair;
                    continue;
                }

                pair.Use();
                Pairs[pair] = true;
                return pair;
            }

            if (fallback != null)
            {
                fallback.Use();
                Pairs[fallback!] = true;
            }

            return fallback;
        }
    }

    /// <summary>
    /// Used by TestPair after checking the server/client pair, Don't use this.
    /// </summary>
    public static void NoCheckReturn(TestPair pair)
    {
        lock (PairLock)
        {
            if (pair.State == TestPair.PairState.Dead)
                Pairs.Remove(pair);
            else if (pair.State == TestPair.PairState.Ready)
                Pairs[pair] = false;
            else
                throw new InvalidOperationException($"Attempted to return a pair in an invalid state. Pair: {pair.Id}. State: {pair.State}.");
        }
    }

    private static void DieIfPoolFailure()
    {
        if (_poolFailureReason != null)
        {
            // If the _poolFailureReason is not null, we can assume at least one test failed.
            // So we say inconclusive so we don't add more failed tests to search through.
            Assert.Inconclusive(@$"
In a different test, the pool manager had an exception when trying to create a server/client pair.
Instead of risking that the pool manager will fail at creating a server/client pairs for every single test,
we are just going to end this here to save a lot of time. This is the exception that started this:\n {_poolFailureReason}");
        }

        if (_dead)
        {
            // If Pairs is null, we ran out of time, we can't assume a test failed.
            // So we are going to tell it all future tests are a failure.
            Assert.Fail("The pool was shut down");
        }
    }

    private static async Task<TestPair> CreateServerClientPair(PoolSettings poolSettings, TextWriter testOut)
    {
        try
        {
            var id = Interlocked.Increment(ref _pairId);
            var pair = new TestPair(id);
            await pair.Initialize(poolSettings, testOut, _testPrototypes);
            pair.Use();
            await pair.RunTicksSync(5);
            await pair.SyncTicks(targetDelta: 1);
            return pair;
        }
        catch (Exception ex)
        {
            _poolFailureReason = ex;
            throw;
        }
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
    /// Initialize the pool manager.
    /// </summary>
    /// <param name="extraAssemblies">Assemblies to search for to discover extra prototypes and systems.</param>
    public static void Startup(params Assembly[] extraAssemblies)
    {
        if (_initialized)
            throw new InvalidOperationException("Already initialized");

        _initialized = true;
        _contentAssemblies =
        [
            typeof(Shared.Entry.EntryPoint).Assembly,
            typeof(Server.Entry.EntryPoint).Assembly,
            typeof(PoolManager).Assembly
        ];
        _contentAssemblies.UnionWith(extraAssemblies);

        _testPrototypes.Clear();
        DiscoverTestPrototypes(typeof(PoolManager).Assembly);
        foreach (var assembly in extraAssemblies)
        {
            DiscoverTestPrototypes(assembly);
        }
    }
}
