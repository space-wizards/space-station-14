using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    sealed class NetworkIdsMatchTest
    {
        [Test]
        public async Task TestConnect()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

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
            await pairTracker.CleanReturnAsync();
        }
    }
}
