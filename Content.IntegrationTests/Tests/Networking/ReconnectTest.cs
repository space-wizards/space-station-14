using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    public sealed class ReconnectTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

            await client.WaitPost(() => IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("disconnect"));

            // Run some ticks for the disconnect to complete and such.
            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Reconnect.
            client.SetConnectTarget(server);

            await client.WaitPost(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.
            await PoolManager.RunTicksSync(pairTracker.Pair, 10);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
            await pairTracker.CleanReturnAsync();
        }
    }
}
