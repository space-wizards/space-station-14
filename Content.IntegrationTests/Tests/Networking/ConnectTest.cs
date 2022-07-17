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
    public sealed class ConnectTest
    {
        [Test]
        public async Task TestConnect()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

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
