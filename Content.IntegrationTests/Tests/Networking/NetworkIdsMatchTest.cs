using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    sealed class NetworkIdsMatchTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestConnect()
        {
            var client = StartClient();
            var server = StartServer();

            await ConnectNetworking(client, server);

            var clientCompFactory = client.ResolveDependency<IComponentFactory>();
            var serverCompFactory = server.ResolveDependency<IComponentFactory>();

            var clientNetComps = clientCompFactory.NetworkedComponents;
            var serverNetComps = serverCompFactory.NetworkedComponents;

            Assert.That(clientNetComps, Is.Not.Null);
            Assert.That(serverNetComps, Is.Not.Null);
            Assert.That(clientNetComps.Count, Is.EqualTo(serverNetComps.Count));

            // Checks that at least Metadata and Transform are registered.
            Assert.That(clientNetComps.Count, Is.GreaterThanOrEqualTo(2));

            for (var netId = 0; netId < clientNetComps.Count; netId++)
            {
                Assert.That(clientNetComps[netId].Name, Is.EqualTo(serverNetComps[netId].Name));
            }
        }

        private static async Task ConnectNetworking(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
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
        }
    }
}
