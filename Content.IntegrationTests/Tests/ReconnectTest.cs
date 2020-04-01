using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class ReconnectTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var client = StartClient();
            var server = StartServer();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Connect.

            client.SetConnectTarget(server);

            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.

            for (var i = 0; i < 10; i++)
            {
                server.RunTicks(1);
                await server.WaitIdleAsync();
                client.RunTicks(1);
                await client.WaitIdleAsync();
            }

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            client.Post(() => IoCManager.Resolve<IClientConsole>().ProcessCommand("disconnect"));

            // Run some ticks for the disconnect to complete and such.
            for (var i = 0; i < 5; i++)
            {
                server.RunTicks(1);
                await server.WaitIdleAsync();
                client.RunTicks(1);
                await client.WaitIdleAsync();
            }

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Reconnect.

            client.SetConnectTarget(server);

            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.

            for (var i = 0; i < 10; i++)
            {
                server.RunTicks(1);
                await server.WaitIdleAsync();
                client.RunTicks(1);
                await client.WaitIdleAsync();
            }

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
        }
    }
}
