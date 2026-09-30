namespace Content.IntegrationTests.Fixtures;

public abstract partial class GameTest
{
    /// <summary>
    ///     All-default-settings PoolSettings, with the client and server disconnected.
    /// </summary>
    protected static PoolSettings PsDisconnected => new()
    {
        Connected = false,
    };
}
