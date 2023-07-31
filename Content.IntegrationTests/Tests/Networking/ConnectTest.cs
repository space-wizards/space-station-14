using System.Linq;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;

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
#pragma warning disable NUnit2045 // Interdependent assertions.
            Assert.That(playerManager.PlayerCount, Is.EqualTo(1));
            Assert.That(playerManager.Sessions.First().Status, Is.EqualTo(SessionStatus.InGame));
#pragma warning restore NUnit2045

            var clEntityManager = client.ResolveDependency<IEntityManager>();
            var svEntityManager = server.ResolveDependency<IEntityManager>();

            var lastSvEntity = svEntityManager.GetEntities().Last();

            Assert.That(clEntityManager.GetComponent<TransformComponent>(lastSvEntity).Coordinates,
                Is.EqualTo(svEntityManager.GetComponent<TransformComponent>(lastSvEntity).Coordinates));

            await pairTracker.CleanReturnAsync();
        }
    }
}
