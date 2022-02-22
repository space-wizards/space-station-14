using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    public sealed class ConnectTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestConnect()
        {
            var client = StartClient();
            var server = StartServer(new ServerContentIntegrationOption
            {
                Pool = false,
                CVarOverrides =
                {
                    {CVars.NetPVS.Name, "false"}
                }
            });

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

            // Basic checks to ensure that they're connected and data got replicated.

            var playerManager = server.ResolveDependency<IPlayerManager>();
            Assert.That(playerManager.PlayerCount, Is.EqualTo(1));
            Assert.That(playerManager.Sessions.First().Status, Is.EqualTo(SessionStatus.InGame));

            var clEntityManager = client.ResolveDependency<IEntityManager>();
            var svEntityManager = server.ResolveDependency<IEntityManager>();

            var lastSvEntity = svEntityManager.GetEntities().Last();

            Assert.That(clEntityManager.GetComponent<TransformComponent>(lastSvEntity).Coordinates,
                Is.EqualTo(svEntityManager.GetComponent<TransformComponent>(lastSvEntity).Coordinates));
        }
    }
}
