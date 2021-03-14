using NUnit.Framework;
using Robust.Client.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    public class ReconnectTest : ContentIntegrationTest
    {
        [Test]
        public void Test()
        {
            var client = StartClient();
            var server = StartServer();

            // Connect.
            client.SetConnectTarget(server);

            client.WaitPost(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.
            RunTicksSync(client, server, 10);

            client.WaitPost(() => IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("disconnect"));

            // Run some ticks for the disconnect to complete and such.
            RunTicksSync(client, server, 5);

            // Reconnect.
            client.SetConnectTarget(server);

            client.WaitPost(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.
            RunTicksSync(client, server, 10);
        }
    }
}
