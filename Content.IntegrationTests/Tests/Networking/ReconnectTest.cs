using Robust.Client.Console;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    public sealed class ReconnectTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

            var host = client.ResolveDependency<IClientConsoleHost>();
            var netManager = client.ResolveDependency<IClientNetManager>();

            await client.WaitPost(() => host.ExecuteCommand("disconnect"));

            // Run some ticks for the disconnect to complete and such.
            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Reconnect.
            client.SetConnectTarget(server);

            await client.WaitPost(() => netManager.ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.
            await PoolManager.RunTicksSync(pairTracker.Pair, 10);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
            await pairTracker.CleanReturnAsync();
        }
    }
}
