using Content.IntegrationTests.Fixtures;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    public sealed class NetworkIdsMatchTest : GameTest
    {
        [Test]
        public async Task TestConnect()
        {
            var pair = Pair;
            var server = pair.Server;
            var client = pair.Client;

            var clientCompFactory = client.ResolveDependency<IComponentFactory>();
            var serverCompFactory = server.ResolveDependency<IComponentFactory>();

            var clientNetComps = clientCompFactory.NetworkedComponents;
            var serverNetComps = serverCompFactory.NetworkedComponents;

            Assert.Multiple(() =>
            {
                Assert.That(clientNetComps, Is.Not.Null);
                Assert.That(serverNetComps, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(clientNetComps, Has.Count.EqualTo(serverNetComps.Count));

                // Checks that at least Metadata and Transform are registered.
                Assert.That(clientNetComps, Has.Count.GreaterThanOrEqualTo(2));
            });

            Assert.Multiple(() =>
            {
                for (var netId = 0; netId < clientNetComps.Count; netId++)
                {
                    Assert.That(clientNetComps[netId].Name, Is.EqualTo(serverNetComps[netId].Name));
                }
            });
        }
    }
}
