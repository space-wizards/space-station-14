using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    public sealed class ReconnectTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var client = StartClient();
            var server = StartServer();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Connect.
            client.SetConnectTarget(server);

            await client.WaitPost(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.
            await RunTicksSync(client, server, 10);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitPost(() => IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("disconnect"));

            // Run some ticks for the disconnect to complete and such.
            await RunTicksSync(client, server, 5);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Reconnect.
            client.SetConnectTarget(server);

            await client.WaitPost(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.
            await RunTicksSync(client, server, 10);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
        }
    }
}
