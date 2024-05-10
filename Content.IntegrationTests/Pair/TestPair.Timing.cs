#nullable enable
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Pair;

// This partial class contains methods for running the server/client pairs for some number of ticks
public sealed partial class TestPair
{
    /// <summary>
    /// Runs the server-client pair in sync
    /// </summary>
    /// <param name="ticks">How many ticks to run them for</param>
    public async Task RunTicksSync(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            await Server.WaitRunTicks(1);
            await Client.WaitRunTicks(1);
        }
    }

    /// <summary>
    /// Runs the server-client pair in sync, but also ensures they are both idle each tick.
    /// </summary>
    /// <param name="runTicks">How many ticks to run</param>
    public async Task ReallyBeIdle(int runTicks = 25)
    {
        for (var i = 0; i < runTicks; i++)
        {
            await Client.WaitRunTicks(1);
            await Server.WaitRunTicks(1);
            for (var idleCycles = 0; idleCycles < 4; idleCycles++)
            {
                await Client.WaitIdleAsync();
                await Server.WaitIdleAsync();
            }
        }
    }

    /// <summary>
    /// Run the server/clients until the ticks are synchronized.
    /// By default the client will be one tick ahead of the server.
    /// </summary>
    public async Task SyncTicks(int targetDelta = 1)
    {
        var sTick = (int)Server.Timing.CurTick.Value;
        var cTick = (int)Client.Timing.CurTick.Value;
        var delta = cTick - sTick;

        if (delta == targetDelta)
            return;
        if (delta > targetDelta)
            await Server.WaitRunTicks(delta - targetDelta);
        else
            await Client.WaitRunTicks(targetDelta - delta);

        sTick = (int)Server.Timing.CurTick.Value;
        cTick = (int)Client.Timing.CurTick.Value;
        delta = cTick - sTick;
        Assert.That(delta, Is.EqualTo(targetDelta));
    }
}