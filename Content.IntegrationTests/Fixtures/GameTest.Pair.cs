using System.Diagnostics.CodeAnalysis;

namespace Content.IntegrationTests.Fixtures;

public abstract partial class GameTest
{
    /// <summary>
    ///     Runs the client and server for the given number of ticks, in lockstep.
    /// </summary>
    /// <remarks>
    ///     Do not use this as a barrier for client-server synchronization, use <see cref="RunUntilSynced"/>.
    /// </remarks>
    public Task RunTicksSync(int ticks)
    {
        return Pair.RunTicksSync(ticks);
    }

    /// <summary>
    ///     Runs the pairs just long enough for PVS to send entities, ensuring the client's current tick is what the
    ///     server's was at call time.
    /// </summary>
    public async Task RunUntilSynced()
    {
        await Pair.RunUntilSynced();
    }

    /// <summary>
    ///     Runs the test pair for a number of (simulated) seconds.
    /// </summary>
    /// <remarks>
    ///     Does not actually take N seconds to evaluate, the game ticks as fast as possible.
    ///     Do not use this as a barrier for client-server synchronization, use <see cref="RunUntilSynced"/>.
    /// </remarks>
    public Task RunSeconds(float seconds)
    {
        return Pair.RunSeconds(seconds);
    }

    /// <summary>
    ///     Creates a map with a single grid consisting of one tile for use during the test.
    /// </summary>
    /// <remarks>
    ///     The map will be deleted automatically during test cleanup (see <see cref="TestPair{TServer,TClient}.Cleanup"/>).
    ///     Use this method instead of Pair.CreateTestMap to ensure that <see cref="TestMap"/> is not null.
    ///     Data about the map can be referenced using <see cref="TestMap"/>.
    /// </remarks>
    /// <returns>Data about the test map. Can also be accessed via <see cref="TestMap"/>.</returns>
    /// <seealso cref="Pair.TestPair.CreateTestMap"/>
    [MemberNotNull(nameof(TestMap))]
    public async Task<TestMapData> CreateTestMap()
    {
        var data = await Pair.CreateTestMap();
        Assume.That(TestMap, Is.Not.Null);
        return data;
    }
}
