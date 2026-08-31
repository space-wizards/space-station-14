#nullable enable
using Content.IntegrationTests.Fixtures;

namespace Content.IntegrationTests.Tests.Networking;

public sealed class ReconnectTest : GameTest
{
    [Test]
    public async Task Test()
    {
        await Client.ExecuteCommand("disconnect");

        // Run some ticks for the disconnect to complete and such.
        await Pair.ReallyBeIdle();

        // Reconnect.
        await Client.Connect(Server);

        // Run some ticks for the handshake to complete and such.
        await Pair.ReallyBeIdle();
    }
}
