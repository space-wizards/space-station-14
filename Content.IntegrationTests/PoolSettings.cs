#nullable enable

namespace Content.IntegrationTests;

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
    /// Use <see cref="Pair.TestPair.IsTestPrototype(Robust.Shared.Prototypes.EntityPrototype)"/> if you need to exclude test prototypees.
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