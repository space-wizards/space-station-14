#nullable enable
using System.Linq;
using System.Reflection;
using Content.IntegrationTests.Pair;
using Content.Shared.CCVar;
using Robust.UnitTesting;

namespace Content.IntegrationTests;

// The static class exist to avoid breaking changes
public static class PoolManager
{
    public static readonly ContentPoolManager Instance = new();
    public const string TestMap = "Empty";

    public static readonly (string cvar, string value)[] TestCvars =
    {
        // @formatter:off
        (CCVars.DatabaseSynchronous.Name,     "true"),
        (CCVars.DatabaseSqliteDelay.Name,     "0"),
        (CCVars.HolidaysEnabled.Name,         "false"),
        (CCVars.GameMap.Name,                 TestMap),
        (CCVars.AdminLogsQueueSendDelay.Name, "0"),
        (CCVars.NPCMaxUpdates.Name,           "999999"),
        (CCVars.GameRoleTimers.Name,          "false"),
        (CCVars.GameRoleLoadoutTimers.Name,   "false"),
        (CCVars.GameRoleWhitelist.Name,       "false"),
        (CCVars.GridFill.Name,                "false"),
        (CCVars.PreloadGrids.Name,            "false"),
        (CCVars.ArrivalsShuttles.Name,        "false"),
        (CCVars.EmergencyShuttleEnabled.Name, "false"),
        (CCVars.ProcgenPreload.Name,          "false"),
        (CCVars.WorldgenEnabled.Name,         "false"),
        (CCVars.GatewayGeneratorEnabled.Name, "false"),
        (CCVars.GameDummyTicker.Name,         "true"),
        (CCVars.GameLobbyEnabled.Name,        "false"),
        (CCVars.ConfigPresetDevelopment.Name, "false"),
        (CCVars.AdminLogsEnabled.Name,        "false"),
        (CCVars.AutosaveEnabled.Name,         "false"),
        (CCVars.MovementMobPushing.Name,      "false"),
        (CCVars.InteractionRateLimitCount.Name, "9999999"),
        (CCVars.InteractionRateLimitPeriod.Name, "0.1"),
        (CCVars.MovementMobPushing.Name, "false"),
    };

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

    public static async Task<TestPair> GetServerClient(
        PoolSettings? settings = null,
        ITestContextLike? testContext = null)
    {
        return await Instance.GetPair(settings, testContext);
    }

    public static void Startup(params Assembly[] extra)
        => Instance.Startup(extra);

    public static void Shutdown() => Instance.Shutdown();
    public static string DeathReport() => Instance.DeathReport();
}

/// <summary>
/// Making clients, and servers is slow, this manages a pool of them so tests can reuse them.
/// </summary>
public sealed class ContentPoolManager : PoolManager<TestPair>
{
    public override PairSettings DefaultSettings =>  new PoolSettings();
    protected override string GetDefaultTestName(ITestContextLike testContext)
    {
        return testContext.FullName.Replace("Content.IntegrationTests.Tests.", "");
    }

    public override void Startup(params Assembly[] extraAssemblies)
    {
        DefaultCvars.AddRange(PoolManager.TestCvars);

        var shared = extraAssemblies
                .Append(typeof(Shared.Entry.EntryPoint).Assembly)
                .Append(typeof(PoolManager).Assembly)
                .ToArray();

        Startup([typeof(Client.Entry.EntryPoint).Assembly],
            [typeof(Server.Entry.EntryPoint).Assembly],
            shared);
    }
}
